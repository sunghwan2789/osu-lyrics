#pragma once

#include "Subject.h"

#include <vector>
#include <concurrent_unordered_map.h>

#include <Windows.h>
#include "bass.h"
#include "Server.h"
#include "Hooker.h"

class HookP : public Subject
{
public:
    HookP() :
        hookerReadFile(L"kernel32.dll", "ReadFile", HookP::ReadFile),
        hookerBASS_ChannelPlay(L"bass.dll", "BASS_ChannelPlay", HookP::BASS_ChannelPlay),
        hookerBASS_ChannelSetPosition(L"bass.dll", "BASS_ChannelSetPosition", HookP::BASS_ChannelSetPosition),
        hookerBASS_ChannelSetAttribute(L"bass.dll", "BASS_ChannelSetAttribute", HookP::BASS_ChannelSetAttribute),
        hookerBASS_ChannelPause(L"bass.dll", "BASS_ChannelPause", HookP::BASS_ChannelPause)
    {
        Instance = this;
        InitializeCriticalSection(&this->hCritiaclSection);
    }
    ~HookP()
    {
        DeleteCriticalSection(&this->hCritiaclSection);
    }

    void Run();
    void Stop();
    void Notify(double, float);
private:
    static BOOL WINAPI ReadFile(HANDLE, LPVOID, DWORD, LPDWORD, LPOVERLAPPED);
    static BOOL BASSDEF(BASS_ChannelPlay)(DWORD, BOOL);
    static BOOL BASSDEF(BASS_ChannelSetPosition)(DWORD, QWORD, DWORD);
    static BOOL BASSDEF(BASS_ChannelSetAttribute)(DWORD, DWORD, float);
    static BOOL BASSDEF(BASS_ChannelPause)(DWORD);

    // 무필요하게 지우는 발상
    // Server처럼 trunk 만들기??
    static HookP *Instance;
    static HookP *GetInstance();

    // what osu! played
    // [ audioPath, beatmapPath ]
    concurrency::concurrent_unordered_map<std::wstring, std::wstring> audioInfo;
    // what osu! is playing
    // [ audioPath, beatmapPath ]
    std::pair<std::wstring, std::wstring> playing;
    // thread safe하지 않은 playing을 참조할 때 사용
    CRITICAL_SECTION hCritiaclSection;

    Hooker<decltype(HookP::ReadFile)> hookerReadFile;
    Hooker<decltype(HookP::BASS_ChannelPlay)> hookerBASS_ChannelPlay;
    Hooker<decltype(HookP::BASS_ChannelSetPosition)> hookerBASS_ChannelSetPosition;
    Hooker<decltype(HookP::BASS_ChannelSetAttribute)> hookerBASS_ChannelSetAttribute;
    Hooker<decltype(HookP::BASS_ChannelPause)> hookerBASS_ChannelPause;
};
