#pragma comment(lib, "Shlwapi.lib")

#include "Observer.h"

#include <cstdio>
#include <cstdlib>
#include <tchar.h>
#include <string>
#include <utility>
#include <functional>
#include <concurrent_unordered_map.h>

#include <Windows.h>
#include <Shlwapi.h>
#include "bass.h"
#include "bass_fx.h"
#include "Hooker.h"
#include "Server.h"

concurrency::concurrent_unordered_map<tstring, tstring> AudioInfo;
std::pair<tstring, tstring> Playing;
CRITICAL_SECTION hMutex;

Hooker<tReadFile> hkrReadFile(_T("kernel32.dll"), "ReadFile", hkReadFile);
BOOL WINAPI hkReadFile(HANDLE hFile, LPVOID lpBuffer, DWORD nNumberOfBytesToRead, LPDWORD lpNumberOfBytesRead, LPOVERLAPPED lpOverlapped)
{
    if (!hkrReadFile.GetFunction()(hFile, lpBuffer, nNumberOfBytesToRead, lpNumberOfBytesRead, lpOverlapped))
    {
        return FALSE;
    }

    TCHAR szFilePath[MAX_PATH];
    DWORD nFilePathLength = GetFinalPathNameByHandle(hFile, szFilePath, MAX_PATH, VOLUME_NAME_DOS);
    //                  1: \\?\D:\Games\osu!\...
    DWORD dwFilePosition = SetFilePointer(hFile, 0, NULL, FILE_CURRENT) - *lpNumberOfBytesRead;
    // 지금 읽는 파일이 비트맵 파일이고 앞부분을 읽었다면 음악 파일 경로 얻기:
    // AudioFilename은 앞부분에 있음 / 파일 핸들 또 열지 말고 일 한 번만 하자!
    if (_tcsnicmp(_T(".osu"), &szFilePath[nFilePathLength - 4], 4) == 0 && dwFilePosition == 0)
    {
        // strtok은 소스를 변형하므로 일단 백업
        // .osu 파일은 UTF-8(Multibyte) 인코딩
        char *buffer = strdup(reinterpret_cast<char *>(lpBuffer));
        for (char *line = strtok(buffer, "\n"); line != NULL; line = strtok(NULL, "\n"))
        {
            if (strnicmp(line, "AudioFilename:", 14) != 0)
            {
                continue;
            }

            TCHAR *szAudioFilePath = _tcsdup(szFilePath);

            // AudioFilename 값 얻기
            TCHAR szAudioFileName[MAX_PATH];
#ifdef UNICODE
            mbstowcs(szAudioFileName, &line[14], MAX_PATH);
#else
            strncpy(szAudioFileName, &line[14], MAX_PATH);
#endif
            StrTrim(szAudioFileName, _T(" \r"));
            PathRemoveFileSpec(szAudioFilePath);
            PathCombine(szAudioFilePath, szAudioFilePath, szAudioFileName);

            // 검색할 때 대소문자 구분하므로 정확한 파일 경로 얻기
            WIN32_FIND_DATA fdata;
            FindClose(FindFirstFile(szAudioFilePath, &fdata));
            PathRemoveFileSpec(szAudioFilePath);
            PathCombine(szAudioFilePath, szAudioFilePath, fdata.cFileName);
#ifdef UNICODE
            // PathCombineW가 \\?\를 제거한다;;
            TCHAR *szAudioFilePathStripped = _tcsdup(szAudioFilePath);
            _tcscpy(szAudioFilePath, _T("\\\\?\\"));
            _tcscat(szAudioFilePath, szAudioFilePathStripped);
            free(szAudioFilePathStripped);
#endif

            AudioInfo.insert({ szAudioFilePath, szFilePath });

            free(szAudioFilePath);
            break;
        }
        free(buffer);
    }
    else
    {
        // [ audioPath, beatmapPath ]
        concurrency::concurrent_unordered_map<tstring, tstring>::iterator info;
        if ((info = AudioInfo.find(szFilePath)) != AudioInfo.end())
        {
            EnterCriticalSection(&hMutex);
            Playing = { info->first.substr(4), info->second.substr(4) };
            LeaveCriticalSection(&hMutex);
        }
    }
    return TRUE;
}


inline long long Now()
{
    long long t;
    GetSystemTimeAsFileTime(reinterpret_cast<LPFILETIME>(&t));
    return t;
}

Hooker<tBASS_ChannelPlay> hkrPlay(_T("bass.dll"), "BASS_ChannelPlay", hkBASS_ChannelPlay);
BOOL BASSDEF(hkBASS_ChannelPlay)(DWORD handle, BOOL restart)
{
    if (!hkrPlay.GetFunction()(handle, restart))
    {
        return FALSE;
    }

    BASS_CHANNELINFO info;
    BASS_ChannelGetInfo(handle, &info);
    if (info.ctype & BASS_CTYPE_STREAM)
    {
        double currentTime = BASS_ChannelBytes2Seconds(handle, BASS_ChannelGetPosition(handle, BASS_POS_BYTE));
        float tempo; BASS_ChannelGetAttribute(handle, BASS_ATTRIB_TEMPO, &tempo);
        cbBASS_Control(Now(), currentTime, tempo);
    }
    return TRUE;
}

Hooker<tBASS_ChannelSetPosition> hkrSetPos(_T("bass.dll"), "BASS_ChannelSetPosition", hkBASS_ChannelSetPosition);
BOOL BASSDEF(hkBASS_ChannelSetPosition)(DWORD handle, QWORD pos, DWORD mode)
{
    if (!hkrSetPos.GetFunction()(handle, pos, mode))
    {
        return FALSE;
    }

    BASS_CHANNELINFO info;
    BASS_ChannelGetInfo(handle, &info);
    if (info.ctype & BASS_CTYPE_STREAM)
    {
        double currentTime = BASS_ChannelBytes2Seconds(handle, pos);
        float tempo; BASS_ChannelGetAttribute(handle, BASS_ATTRIB_TEMPO, &tempo);
        // 주의!! pos가 일정 이하일 때,
        // 재생하면 BASS_ChannelPlay대신 이 함수가 호출되고,
        // BASS_ChannelIsActive 값은 BASS_ACTIVE_PAUSED임.
        if (BASS_ChannelIsActive(handle) == BASS_ACTIVE_PAUSED)
        {
            tempo = -100;
        }
        cbBASS_Control(Now(), currentTime, tempo);
    }
    return TRUE;
}

Hooker<tBASS_ChannelSetAttribute> hkrSetAttr(_T("bass.dll"), "BASS_ChannelSetAttribute", hkBASS_ChannelSetAttribute);
BOOL BASSDEF(hkBASS_ChannelSetAttribute)(DWORD handle, DWORD attrib, float value)
{
    if (!hkrSetAttr.GetFunction()(handle, attrib, value))
    {
        return FALSE;
    }

    BASS_CHANNELINFO info;
    BASS_ChannelGetInfo(handle, &info);
    if ((info.ctype & BASS_CTYPE_STREAM) && attrib == BASS_ATTRIB_TEMPO)
    {
        double currentTime = BASS_ChannelBytes2Seconds(handle, BASS_ChannelGetPosition(handle, BASS_POS_BYTE));
        cbBASS_Control(Now(), currentTime, value);
    }
    return TRUE;
}

Hooker<tBASS_ChannelPause> hkrPause(_T("bass.dll"), "BASS_ChannelPause", hkBASS_ChannelPause);
BOOL BASSDEF(hkBASS_ChannelPause)(DWORD handle)
{
    if (!hkrPause.GetFunction()(handle))
    {
        return FALSE;
    }

    BASS_CHANNELINFO info;
    BASS_ChannelGetInfo(handle, &info);
    if (info.ctype & BASS_CTYPE_STREAM)
    {
        double currentTime = BASS_ChannelBytes2Seconds(handle, BASS_ChannelGetPosition(handle, BASS_POS_BYTE));
        cbBASS_Control(Now(), currentTime, -100);
    }
    return TRUE;
}


inline void cbBASS_Control(long long calledAt, double currentTime, float tempo)
{
    TCHAR message[nBufferSize];
    tstring audioPath, beatmapPath;
    EnterCriticalSection(&hMutex);
    std::tie(audioPath, beatmapPath) = Playing;
    LeaveCriticalSection(&hMutex);
    _stprintf(message, _T("%llx|%s|%lf|%f|%s\n"), calledAt, audioPath.c_str(), currentTime, tempo, beatmapPath.c_str());
    PushMessage(message);
}


void RunObserver()
{
    InitializeCriticalSection(&hMutex);
    hkrReadFile.Hook();

    hkrPlay.Hook();
    hkrSetAttr.Hook();
    hkrSetPos.Hook();
    hkrPause.Hook();
}

void StopObserver()
{
    hkrPause.Unhook();
    hkrSetAttr.Unhook();
    hkrSetPos.Unhook();
    hkrPlay.Unhook();

    hkrReadFile.Unhook();
    DeleteCriticalSection(&hMutex);
}
