#include "Core.h"

#include <Windows.h>
#include <Shlwapi.h>
#include <map>
#include <cstdio>
#include <mutex>

#pragma comment(lib, "Shlwapi.lib")

std::wstring                            g_audioPath;
std::wstring                            g_beatmapPath;

std::map<std::wstring, std::wstring>    g_songCached;
std::mutex                              g_songMutex;

NamedPipe                               g_namedPipe
;

//
// Proxy 정의부. (#define 으로 정의되어있음.)
//      MAKE_PROXY_DEF(module, function, proxy)
//      HookEngine<decltype(proxy)> proxy__hk = HookEngine<decltype(proxy)>(module, function, proxy)
// EXAMPLE : 
//      MAKE_PROXY_DEF(NAME_KERNEL_DLL, "ReadFile", proxyReadFile);
//      ==> HookEngine< decltype(proxyReadFile) > proxyReadFile__hk = HookEngine< decltype(proxy) >(module, function, proxy);
//

MAKE_PROXY_DEF(NAME_KERNEL_DLL, "ReadFile",                 proxyReadFile);
MAKE_PROXY_DEF(NAME_BASS_DLL,   "BASS_ChannelPlay",         proxyBASS_ChannelPlay);
MAKE_PROXY_DEF(NAME_BASS_DLL,   "BASS_ChannelSetPosition",  proxyBASS_ChannelSetPosition);
MAKE_PROXY_DEF(NAME_BASS_DLL,   "BASS_ChannelSetAttribute", proxyBASS_ChannelSetAttribute);
MAKE_PROXY_DEF(NAME_BASS_DLL,   "BASS_ChannelPause",        proxyBASS_ChannelPause);

void Start()
{
    BeginHook();                                // Begin Hooking Method.
    EngineHook(proxyReadFile);                      // ReadFile Hook.
    EngineHook(proxyBASS_ChannelPlay);              // ChannelPlay Hook.
    EngineHook(proxyBASS_ChannelSetPosition);       // ChannelSetPosition Hook.
    EngineHook(proxyBASS_ChannelSetAttribute);      // ChannelSetAttribute Hook.
    EngineHook(proxyBASS_ChannelPause);             // ChannelPause Hook.
    EndHook();                                  // End Hooking Method.

    g_namedPipe.Start(NAME_NAMED_PIPE);
}

void Stop()
{
    BeginHook();                                // Begin Hooking Method.
    EngineUnhook(proxyReadFile);                    // ReadFile Unhook.
    EngineUnhook(proxyBASS_ChannelPlay);            // ChannelPlay Unhook.
    EngineUnhook(proxyBASS_ChannelSetPosition);     // ChannelSetPostion Unhook.
    EngineUnhook(proxyBASS_ChannelSetAttribute);    // ChannelSetAttribute Unhook.
    EngineUnhook(proxyBASS_ChannelPause);           // ChannelPause Unhook.
    EndHook();                                  // End Hooking Method.
    
    g_namedPipe.Stop();
}

//
// TODO : currentTime과 tempo가 무엇을 의미하는지 알수 없습니다.
//        주석으로 정의 부탁드립니다.
//
void Notify(double currentTime, float tempo)
{
    wchar_t message[MAX_MESSAGE_LENGTH];

    FILETIME ft;
    GetSystemTimeAsFileTime(&ft);

    // 지금 무엇을 플레이하고있는지 proxyReadFile 에서 얻어낸 값을 클라이언트로 전송합니다.
    g_songMutex.lock();
    swprintf(message, L"%llx|%s|%lf|%f|%s\n",
        ((long long)ft.dwHighDateTime << 32) + ft.dwLowDateTime,
        g_audioPath.c_str(),
        currentTime,
        tempo,
        g_beatmapPath.c_str());
    g_songMutex.unlock();

    g_namedPipe.PushMessage(std::wstring(message));
}

BOOL WINAPI proxyReadFile(HANDLE hFile, LPVOID lpBuffer, DWORD nNumberOfBytesToRead, LPDWORD lpNumberOfBytesRead, LPOVERLAPPED lpOverlapped)
{
    if (!SelectProxy(proxyReadFile).OriginalFunction(hFile, lpBuffer, nNumberOfBytesToRead, lpNumberOfBytesRead, lpOverlapped))
    {
        return FALSE;
    }

    
    TCHAR nameTmpFilePath[MAX_PATH];

    DWORD dwTmpFilePathLength = GetFinalPathNameByHandle(hFile, nameTmpFilePath, MAX_PATH, VOLUME_NAME_DOS);
    DWORD dwFilePosition = SetFilePointer(hFile, 0, NULL, FILE_CURRENT) - (*lpNumberOfBytesRead);

    // 앞의 //?/를 제거하기위해 4부터 시작함.
    TCHAR* nameFile = &nameTmpFilePath[4];
    DWORD  dwFilePathLength = dwTmpFilePathLength - 4;

    // 지금 읽는 파일이 비트맵 파일이고 앞부분을 읽었다면 음악 파일 경로 얻기:
    // 파일 이름이 포함된 Path 끝부분 4글자를 자름. 4글자와  .osu를 비교하여 이 파일이 osu 파일인지 확인함
    if (wcsncmp(L".osu", &nameFile[dwFilePathLength - 4], 4) == 0 && dwFilePosition == 0)
    {
        // .osu 파일은 UTF-8(Multibyte) 인코딩
        // strtok은 소스를 변형하므로 먼저 strdup를 이용해서 글자를 복사함.
        LPSTR buffer = strdup((const char*)(lpBuffer));

        // for문을 이용해서 줄마다 strtok으로 잘라냄.
        for (LPSTR line = strtok(buffer, "\n"); line != NULL; line = strtok(NULL, "\n"))
        {
            // 잘라낸 줄에 Token이 있는지 확인하고 아닐경우에는 continue함.
            if (strnicmp(line, AUDIO_FILE_INFO_TOKEN, strlen(AUDIO_FILE_INFO_TOKEN)) != 0)
            {
                continue;
            }

            TCHAR nameAudioFile[MAX_PATH];
            mbstowcs(nameAudioFile, &line[strlen(AUDIO_FILE_INFO_TOKEN)], MAX_PATH);
            StrTrimW(nameAudioFile, L" \r");

            TCHAR pathAudioFile[MAX_PATH];
            wcscpy(pathAudioFile, nameFile);

            // pathAudioFile에서 파일명을 지웁니다.
            PathRemoveFileSpecW(pathAudioFile);

            // 파일명이 지워진 Path인 pathAudioFile에 nameAudioFile붙여, 완전한 Path를 만듭니다.
            PathCombineW(pathAudioFile, pathAudioFile, nameAudioFile);

            g_songMutex.lock();
            g_beatmapPath  = (std::wstring(nameFile));
            g_audioPath    = (std::wstring(pathAudioFile));

            if (g_songCached.find(g_audioPath) == g_songCached.end())
            {
                g_songCached.insert(
                    std::pair<std::wstring, std::wstring>(pathAudioFile, nameFile));
            }

            g_songMutex.unlock();

            break;
        }

        // strdup를 이용해 복사한 문자열의 메모리를 해제시킵니다.
        free(buffer);
    }
    else
    {
        // Beatmap을 다시 불러오지 않고 Osu내부의 메모리 Cache에서 불러올때가 있는데.                         
        // 그럴경우에는 Audio파일만 읽으니 기존에 동일한 Beatmap의 Audio파일이 기존에 
        // 읽힌적이 있는지 확인하고 현재 실행되고있는 곡을 지금 읽은 AudioFile으로 바꾼다.                            
        g_songMutex.lock();
        auto cachedInfo = g_songCached.find(nameFile);
        
        if (cachedInfo != g_songCached.end())
        {
            g_audioPath   = cachedInfo->first;
            g_beatmapPath = cachedInfo->second;
        }

        g_songMutex.unlock();
    }
    return TRUE;
    
}

BOOL WINAPI proxyBASS_ChannelPlay(DWORD handle, BOOL restart)
{
    if (!SelectProxy(proxyBASS_ChannelPlay).OriginalFunction(handle, restart))
    {
        return FALSE;
    }

    BASS_CHANNELINFO info;
    BASS_ChannelGetInfo(handle, &info);

    if (info.ctype & BASS_CTYPE_STREAM)
    {
        double currentTime = BASS_ChannelBytes2Seconds(handle, BASS_ChannelGetPosition(handle, BASS_POS_BYTE));
        float currentTempo = 0;
        BASS_ChannelGetAttribute(handle, BASS_ATTRIB_TEMPO, &currentTempo);

        Notify(currentTime, currentTempo);
    }
    return TRUE;

}

BOOL WINAPI proxyBASS_ChannelSetPosition(DWORD handle, QWORD pos, DWORD mode)
{
    if (!SelectProxy(proxyBASS_ChannelSetPosition).OriginalFunction(handle, pos, mode))
    {
        return FALSE;
    }

    BASS_CHANNELINFO info;
    BASS_ChannelGetInfo(handle, &info);

    if (info.ctype & BASS_CTYPE_STREAM)
    {
        double currentTime = BASS_ChannelBytes2Seconds(handle, pos);
        float currentTempo = 0;
        BASS_ChannelGetAttribute(handle, BASS_ATTRIB_TEMPO, &currentTempo);
        // 주의!! pos가 일정 이하일 때,
        // 재생하면 proxyBASS_ChannelPlay대신 이 함수가 호출되고,
        // BASS_ChannelIsActive 값은 BASS_ACTIVE_PAUSED임.
        if (BASS_ChannelIsActive(handle) == BASS_ACTIVE_PAUSED)
        {
            currentTempo = -100;
        }

        Notify(currentTime, currentTempo);
    }

    return TRUE;
}

BOOL WINAPI proxyBASS_ChannelSetAttribute(DWORD handle, DWORD attrib, float value)
{
    if (!SelectProxy(proxyBASS_ChannelSetAttribute).OriginalFunction(handle, attrib, value))
    {
        return FALSE;
    }
    BASS_CHANNELINFO info;
    BASS_ChannelGetInfo(handle, &info);

    if ((info.ctype & BASS_CTYPE_STREAM) && attrib == BASS_ATTRIB_TEMPO)
    {
        double currentTime = BASS_ChannelBytes2Seconds(handle, BASS_ChannelGetPosition(handle, BASS_POS_BYTE));

        Notify(currentTime, value);
    }

    return TRUE;
}

BOOL WINAPI proxyBASS_ChannelPause(DWORD handle)
{
    if (!SelectProxy(proxyBASS_ChannelPause).OriginalFunction(handle))
    {
        return FALSE;
    }

    BASS_CHANNELINFO info;
    BASS_ChannelGetInfo(handle, &info);
    if (info.ctype & BASS_CTYPE_STREAM)
    {
        double currentTime = BASS_ChannelBytes2Seconds(handle, BASS_ChannelGetPosition(handle, BASS_POS_BYTE));

        Notify(currentTime, -100);
    }

    return TRUE;
}


BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved)
{
    switch (fdwReason)
    {
    case DLL_PROCESS_ATTACH: 
        // 
        // LoaderLock의 데드락을 막기 위해서 스레드로 호출한다.
        //
        CreateThread(0, 0, (LPTHREAD_START_ROUTINE)Start, 0, 0, 0);
        break;
    case DLL_PROCESS_DETACH: 
        Stop(); 
        break;
    }
    return TRUE;
}