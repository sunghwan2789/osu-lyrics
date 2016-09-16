#pragma once

#include "Subject.h"

#include <concurrent_unordered_map.h>

#include <Windows.h>
#include "bass.h"
#include "bass_fx.h"
#include "Hooker.h"

class Monitor : public Subject
{
public:
    Monitor();
    ~Monitor();

    void Activate();
    void Disable();
    void Notify(double, float);

private:
    BOOL OnReadFile(HANDLE, LPVOID, DWORD, LPDWORD, LPOVERLAPPED);
    BOOL OnBASS_ChannelPlay(DWORD, BOOL);
    BOOL OnBASS_ChannelSetPosition(DWORD, QWORD, DWORD);
    BOOL OnBASS_ChannelSetAttribute(DWORD, DWORD, float);
    BOOL OnBASS_ChannelPause(DWORD);

    Hooker<decltype(ReadFile)> hookerReadFile;
    Hooker<decltype(BASS_ChannelPlay)> hookerBASS_ChannelPlay;
    Hooker<decltype(BASS_ChannelSetPosition)> hookerBASS_ChannelSetPosition;
    Hooker<decltype(BASS_ChannelSetAttribute)> hookerBASS_ChannelSetAttribute;
    Hooker<decltype(BASS_ChannelPause)> hookerBASS_ChannelPause;

    // what osu! played
    // [ audioPath, beatmapPath ]
    concurrency::concurrent_unordered_map<std::wstring, std::wstring> audioInfo;
    // what osu! is playing
    // [ audioPath, beatmapPath ]
    std::pair<std::wstring, std::wstring> playing;
    // thread safe하지 않은 playing을 참조할 때 사용
    CRITICAL_SECTION hCritiaclSection;
};
