#pragma once

#include <memory>
#include <mutex>
#include <concurrent_unordered_map.h>

#include <Windows.h>
#include "bass.h"
#include "Server.h"
#include "Hooker.h"

class Observer
{
public:
    void Run();
    void Stop();

private:
    static BOOL WINAPI ReadFile(HANDLE, LPVOID, DWORD, LPDWORD, LPOVERLAPPED);
    static BOOL BASSDEF(BASS_ChannelPlay)(DWORD, BOOL);
    static BOOL BASSDEF(BASS_ChannelSetPosition)(DWORD, QWORD, DWORD);
    static BOOL BASSDEF(BASS_ChannelSetAttribute)(DWORD, DWORD, float);
    static BOOL BASSDEF(BASS_ChannelPause)(DWORD);

    void Report_BASS(long long, double, float);
    CRITICAL_SECTION hCritiaclSection;

    concurrency::concurrent_unordered_map<tstring, tstring> audioInfo;
    std::pair<tstring, tstring> playing;

    Hooker<decltype(Observer::ReadFile)> hookerReadFile;
    Hooker<decltype(Observer::BASS_ChannelPlay)> hookerBASS_ChannelPlay;
    Hooker<decltype(Observer::BASS_ChannelSetPosition)> hookerBASS_ChannelSetPosition;
    Hooker<decltype(Observer::BASS_ChannelSetAttribute)> hookerBASS_ChannelSetAttribute;
    Hooker<decltype(Observer::BASS_ChannelPause)> hookerBASS_ChannelPause;

public:
    static Observer *GetInstance()
    {
        std::call_once(Observer::once_flag, []
        {
            Observer::instance.reset(new Observer, [](Observer *p)
            {
                delete p;
            });
        });
        return Observer::instance.get();
    }

private:
    Observer() : hookerReadFile(_T("kernel32.dll"), "ReadFile", Observer::ReadFile),
        hookerBASS_ChannelPlay(_T("bass.dll"), "BASS_ChannelPlay", Observer::BASS_ChannelPlay),
        hookerBASS_ChannelSetPosition(_T("bass.dll"), "BASS_ChannelSetPosition", Observer::BASS_ChannelSetPosition),
        hookerBASS_ChannelSetAttribute(_T("bass.dll"), "BASS_ChannelSetAttribute", Observer::BASS_ChannelSetAttribute),
        hookerBASS_ChannelPause(_T("bass.dll"), "BASS_ChannelPause", Observer::BASS_ChannelPause)
    {
        InitializeCriticalSection(&this->hCritiaclSection);
    }
    ~Observer()
    {
        DeleteCriticalSection(&this->hCritiaclSection);
    }

    Observer(const Observer&) = delete;
    Observer(Observer&&) = delete;
    Observer& operator=(const Observer&) = delete;
    Observer& operator=(Observer&&) = delete;

    static std::shared_ptr<Observer> instance;
    static std::once_flag once_flag;
};
