#pragma comment (lib, "Shlwapi.lib")

#include <Windows.h>
#include <utility>
#include <string>
#include <cstdio>
#include <concurrent_unordered_map.h>
#include <cstring>
#include <Shlwapi.h>
#include "bass.h"
#include "bass_fx.h"
#include "Hooker.h"
#include "Server.h"
#include "Observer.h"

concurrency::concurrent_unordered_map<std::string, std::string> AudioInfo;
std::pair<std::string, std::string> Playing;
CRITICAL_SECTION hMutex;

Hooker<tReadFile> hkrReadFile("kernel32.dll", "ReadFile", hkReadFile);
BOOL WINAPI hkReadFile(HANDLE hFile, LPVOID lpBuffer, DWORD nNumberOfBytesToRead, LPDWORD lpNumberOfBytesRead, LPOVERLAPPED lpOverlapped)
{
    if (!hkrReadFile.pOriginalFunction(hFile, lpBuffer, nNumberOfBytesToRead, lpNumberOfBytesRead, lpOverlapped))
    {
        return FALSE;
    }

    char path[MAX_PATH];
    DWORD pathLength = GetFinalPathNameByHandle(hFile, path, MAX_PATH, VOLUME_NAME_DOS);
    //                  1: \\?\D:\Games\osu!\...
    DWORD seekPosition = SetFilePointer(hFile, 0, NULL, FILE_CURRENT) - *lpNumberOfBytesRead;
    // 지금 읽는 파일이 비트맵 파일이고 앞부분을 읽었다면:
    // AudioFilename은 앞부분에 있음 / 파일 핸들 또 열지 말고 일 한 번만 하자!
    if (strnicmp(".osu", &path[pathLength - 4], 4) == 0 && seekPosition == 0)
    {
        // strtok은 소스를 변형하므로 일단 백업
        char *buffer = strdup((char *) lpBuffer);
        for (char *line = strtok(buffer, "\n"); line != NULL; line = strtok(NULL, "\n"))
        {
            // 비트맵의 음악 파일 경로 얻기
            if (strnicmp(line, "AudioFilename:", 14) != 0)
            {
                continue;
            }

            char *beatmapDir = strdup(path);
            PathRemoveFileSpec(beatmapDir);

            char audioPath[MAX_PATH];

            // get value & trim
            int i = 14;
            for (; line[i] == ' '; i++);
            buffer[0] = NULL;
            strncat(buffer, &line[i], strlen(line) - i - 1);
            PathCombine(audioPath, beatmapDir, buffer);

            // 검색할 때 대소문자 구분하므로 제대로 된 파일 경로 얻기
            WIN32_FIND_DATA fdata;
            FindClose(FindFirstFile(audioPath, &fdata));
            PathRemoveFileSpec(audioPath);
            PathCombine(audioPath, audioPath, fdata.cFileName);

            AudioInfo.insert({ audioPath, path });

            free(beatmapDir);
            break;
        }
        free(buffer);
    }
    else
    {
        // [ audioPath, beatmapPath ]
        concurrency::concurrent_unordered_map<std::string, std::string>::iterator info;
        if ((info = AudioInfo.find(path)) != AudioInfo.end())
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
    GetSystemTimeAsFileTime((LPFILETIME) &t);
    return t;
}

Hooker<tBASS_ChannelPlay> hkrPlay("bass.dll", "BASS_ChannelPlay", hkBASS_ChannelPlay);
BOOL BASSDEF(hkBASS_ChannelPlay)(DWORD handle, BOOL restart)
{
    if (!hkrPlay.pOriginalFunction(handle, restart))
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

Hooker<tBASS_ChannelSetPosition> hkrSetPos("bass.dll", "BASS_ChannelSetPosition", hkBASS_ChannelSetPosition);
BOOL BASSDEF(hkBASS_ChannelSetPosition)(DWORD handle, QWORD pos, DWORD mode)
{
    if (!hkrSetPos.pOriginalFunction(handle, pos, mode))
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

Hooker<tBASS_ChannelSetAttribute> hkrSetAttr("bass.dll", "BASS_ChannelSetAttribute", hkBASS_ChannelSetAttribute);
BOOL BASSDEF(hkBASS_ChannelSetAttribute)(DWORD handle, DWORD attrib, float value)
{
    if (!hkrSetAttr.pOriginalFunction(handle, attrib, value))
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

Hooker<tBASS_ChannelPause> hkrPause("bass.dll", "BASS_ChannelPause", hkBASS_ChannelPause);
BOOL BASSDEF(hkBASS_ChannelPause)(DWORD handle)
{
    if (!hkrPause.pOriginalFunction(handle))
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
    char message[BUF_SIZE];
    EnterCriticalSection(&hMutex);
    sprintf(message, "%llx|%s|%lf|%f|%s\n", calledAt, Playing.first.c_str(), currentTime, tempo, Playing.second.c_str());
    LeaveCriticalSection(&hMutex);
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
