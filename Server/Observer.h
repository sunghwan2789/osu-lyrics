#pragma once

#include <memory>
#include <mutex>
#include <list>

#include <Windows.h>
#include "bass.h"
#include "Server.h"
#include "Hooker.h"

/* 프록시 함수들의 정의구. */
BOOL WINAPI proxyReadFile(HANDLE, LPVOID, DWORD, LPDWORD, LPOVERLAPPED);
BOOL BASSDEF(proxyBASS_ChannelPlay)(DWORD, BOOL);
BOOL BASSDEF(proxyBASS_ChannelSetPosition)(DWORD, QWORD, DWORD);
BOOL BASSDEF(proxyBASS_ChannelSetAttribute)(DWORD, DWORD, float);
BOOL BASSDEF(proxyBASS_ChannelPause)(DWORD);

class Observer
{
public:
    void Start();
    void Stop();

    void SendInfomation(long long, double, float);
    CRITICAL_SECTION hCritiaclSection;

	struct SongInfo {
		std::wstring audioPath;
		std::wstring beatmapPath;
	} currentPlaying;

    std::list<SongInfo> listBeatmapCached;
    bool isSongCached(TCHAR* nameSongPath);

    Hooker<decltype(proxyReadFile)> hookerReadFile;
    Hooker<decltype(proxyBASS_ChannelPlay)> hookerBASS_ChannelPlay;
    Hooker<decltype(proxyBASS_ChannelSetPosition)> hookerBASS_ChannelSetPosition;
    Hooker<decltype(proxyBASS_ChannelSetAttribute)> hookerBASS_ChannelSetAttribute;
    Hooker<decltype(proxyBASS_ChannelPause)> hookerBASS_ChannelPause;

    Observer() : 
        hookerReadFile(L"kernel32.dll", "ReadFile", proxyReadFile),
        hookerBASS_ChannelPlay(L"bass.dll", "BASS_ChannelPlay", proxyBASS_ChannelPlay),
        hookerBASS_ChannelSetPosition(L"bass.dll", "BASS_ChannelSetPosition", proxyBASS_ChannelSetPosition),
        hookerBASS_ChannelSetAttribute(L"bass.dll", "BASS_ChannelSetAttribute", proxyBASS_ChannelSetAttribute),
        hookerBASS_ChannelPause(L"bass.dll", "BASS_ChannelPause", proxyBASS_ChannelPause)
    {
        InitializeCriticalSection(&hCritiaclSection);
        Start();
    }
    ~Observer()
    {
        Stop();
        DeleteCriticalSection(&hCritiaclSection);
    }

    Observer(const Observer&) = delete;
    Observer(Observer&&) = delete;
    Observer& operator=(const Observer&) = delete;
    Observer& operator=(Observer&&) = delete;
};
