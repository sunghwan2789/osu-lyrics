#pragma comment(lib, "Shlwapi.lib")

#include "Observer.h"

#include <cstdio>
#include <cstdlib>
#include <cwchar>
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

std::shared_ptr<Observer> Observer::instance;
std::once_flag Observer::once_flag;

BOOL WINAPI Observer::ReadFile(HANDLE hFile, LPVOID lpBuffer, DWORD nNumberOfBytesToRead, LPDWORD lpNumberOfBytesRead, LPOVERLAPPED lpOverlapped)
{
    Observer *self = Observer::GetInstance();
    if (!self->hookerReadFile.GetFunction()(hFile, lpBuffer, nNumberOfBytesToRead, lpNumberOfBytesRead, lpOverlapped))
    {
        return FALSE;
    }

    // osu!는 긴 파일 이름(260자 이상)을 지원하지 않으므로,
    // szFilePath 길이는 MAX_PATH로 하고 추가 할당하는 로직은 제외
    // GetFinalPathNameByHandle 스펙상, szFilePath 값은 \\?\D:\Games\osu!\...
    wchar_t szFilePath[MAX_PATH];
    DWORD nFilePathLength = GetFinalPathNameByHandle(hFile, szFilePath, MAX_PATH, VOLUME_NAME_DOS);
    // 읽는 게 디스크 파일이 아니거나, osu!의 능력을 초과한 기타 library의 작업?
    if (nFilePathLength == 0 || nFilePathLength > MAX_PATH || lpNumberOfBytesRead == NULL)
    {
        return TRUE;
    }

    DWORD dwFilePosition = SetFilePointer(hFile, 0, NULL, FILE_CURRENT) - *lpNumberOfBytesRead;
    // 지금 읽는 파일이 비트맵 파일이고 앞부분을 읽었다면 음악 파일 경로 얻기:
    // AudioFilename은 앞부분에 있음 / 파일 핸들 또 열지 말고 일 한 번만 하자!
    if (wcsnicmp(L".osu", &szFilePath[nFilePathLength - 4], 4) == 0 && dwFilePosition == 0)
    {
        // strtok은 소스를 변형하므로 일단 백업
        // .osu 파일은 UTF-8(Multibyte) 인코딩
        char *buffer = strdup((const char *) lpBuffer);
        for (char *line = strtok(buffer, "\n"); line != NULL; line = strtok(NULL, "\n"))
        {
            if (strnicmp(line, "AudioFilename:", 14) != 0)
            {
                continue;
            }

            // AudioFilename 값 얻기
            wchar_t szAudioFileName[MAX_PATH];
            mbstowcs(szAudioFileName, &line[14], MAX_PATH);
            StrTrim(szAudioFileName, L" \r");

            // 비트맵 파일을 기준으로 음악 파일의 경로 찾기
            wchar_t szAudioFilePath[MAX_PATH];
            wcscpy(szAudioFilePath, szFilePath);
            PathRemoveFileSpec(szAudioFilePath);
            PathCombine(szAudioFilePath, szAudioFilePath, szAudioFileName);

            // audioInfo에서 파일 정보를 검색할 때 대소문자 구분하므로 정확한 파일 경로 얻기
            WIN32_FIND_DATA fdata;
            FindClose(FindFirstFile(szAudioFilePath, &fdata));
            PathRemoveFileSpec(szAudioFilePath);
            PathCombine(szAudioFilePath, szAudioFilePath, fdata.cFileName);

            // PathCombineW가 \\?\(Long Unicode path prefix)를 제거하는데,
            // GetFinalPathNameByHandle 스펙에 맞게 다시 추가해서
            // 음악이 바뀜을 감지할 때 덜 혼란스럽게 하자
            wcscpy(szAudioFileName, szAudioFilePath);
            wcscpy(szAudioFilePath, L"\\\\?\\");
            wcscat(szAudioFilePath, szAudioFileName);

            // osu!에서 비트맵을 바꿀 때 매번 비트맵 파일을 읽지 않고 캐시에서 불러오기도 함
            // => 비트맵 파일보다는 음악 파일을 읽을 때 재생 정보 갱신해야
            self->audioInfo.insert({szAudioFilePath, szFilePath});
            break;
        }
        free(buffer);
    }
    // 지금 읽는 파일이 비트맵 음악 파일일 때 재생 정보 갱신하기
    else
    {
        decltype(self->audioInfo)::iterator info;
        if ((info = self->audioInfo.find(szFilePath)) != self->audioInfo.end())
        {
            EnterCriticalSection(&self->hCritiaclSection);
            self->playing = {info->first.substr(4), info->second.substr(4)};
            LeaveCriticalSection(&self->hCritiaclSection);
        }
    }
    return TRUE;
}


inline long long GetSystemTimeAsFileTime()
{
    /*
    * Do not cast a pointer to a FILETIME structure to either a
    * ULARGE_INTEGER* or __int64* value because it can cause alignment faults on 64-bit Windows.
    * via  http://technet.microsoft.com/en-us/library/ms724284(v=vs.85).aspx
    */
    FILETIME ft;
    GetSystemTimeAsFileTime(&ft);
    return ((long long) ft.dwHighDateTime << 32) + ft.dwLowDateTime;
}

BOOL WINAPI Observer::BASS_ChannelPlay(DWORD handle, BOOL restart)
{
    Observer *self = Observer::GetInstance();
    if (!self->hookerBASS_ChannelPlay.GetFunction()(handle, restart))
    {
        return FALSE;
    }

    BASS_CHANNELINFO info;
    BASS_ChannelGetInfo(handle, &info);
    if (info.ctype & BASS_CTYPE_STREAM)
    {
        double currentTime = BASS_ChannelBytes2Seconds(handle, BASS_ChannelGetPosition(handle, BASS_POS_BYTE));
        float tempo; BASS_ChannelGetAttribute(handle, BASS_ATTRIB_TEMPO, &tempo);
        self->Update(currentTime, tempo);
    }
    return TRUE;
}

BOOL WINAPI Observer::BASS_ChannelSetPosition(DWORD handle, QWORD pos, DWORD mode)
{
    Observer *self = Observer::GetInstance();
    if (!self->hookerBASS_ChannelSetPosition.GetFunction()(handle, pos, mode))
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
        self->Update(currentTime, tempo);
    }
    return TRUE;
}

BOOL WINAPI Observer::BASS_ChannelSetAttribute(DWORD handle, DWORD attrib, float value)
{
    Observer *self = Observer::GetInstance();
    if (!self->hookerBASS_ChannelSetAttribute.GetFunction()(handle, attrib, value))
    {
        return FALSE;
    }

    BASS_CHANNELINFO info;
    BASS_ChannelGetInfo(handle, &info);
    if ((info.ctype & BASS_CTYPE_STREAM) && attrib == BASS_ATTRIB_TEMPO)
    {
        double currentTime = BASS_ChannelBytes2Seconds(handle, BASS_ChannelGetPosition(handle, BASS_POS_BYTE));
        self->Update(currentTime, value);
    }
    return TRUE;
}

BOOL WINAPI Observer::BASS_ChannelPause(DWORD handle)
{
    Observer *self = Observer::GetInstance();
    if (!self->hookerBASS_ChannelPause.GetFunction()(handle))
    {
        return FALSE;
    }

    BASS_CHANNELINFO info;
    BASS_ChannelGetInfo(handle, &info);
    if (info.ctype & BASS_CTYPE_STREAM)
    {
        double currentTime = BASS_ChannelBytes2Seconds(handle, BASS_ChannelGetPosition(handle, BASS_POS_BYTE));
        self->Update(currentTime, -100);
    }
    return TRUE;
}

void Observer::Update(double currentTime, float tempo)
{
    Observer *self = Observer::GetInstance();
    wchar_t message[Server::nMessageLength];
    EnterCriticalSection(&self->hCritiaclSection);
    swprintf(message, L"%llx|%s|%lf|%f|%s\n",
        GetSystemTimeAsFileTime(),
        self->playing.first.c_str(),
        currentTime,
        tempo,
        self->playing.second.c_str()
    );
    LeaveCriticalSection(&self->hCritiaclSection);
    Server::GetInstance()->PushMessage(message);
}

void Observer::Run()
{
    this->hookerReadFile.Hook();

    this->hookerBASS_ChannelPlay.Hook();
    this->hookerBASS_ChannelSetPosition.Hook();
    this->hookerBASS_ChannelSetAttribute.Hook();
    this->hookerBASS_ChannelPause.Hook();
}

void Observer::Stop()
{
    this->hookerBASS_ChannelPause.Unhook();
    this->hookerBASS_ChannelSetAttribute.Unhook();
    this->hookerBASS_ChannelSetPosition.Unhook();
    this->hookerBASS_ChannelPlay.Unhook();

    this->hookerReadFile.Unhook();
}
