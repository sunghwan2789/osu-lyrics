#pragma once

#include "Defines.h"
#include "detours.h"
#include "bass.h"
#include "bass_fx.h"

#include <atomic>
#include <thread>
#include <concurrent_queue.h>

//
// proxy 헤더 정의부.
//

BOOL WINAPI 
proxyReadFile(                  HANDLE hFile, 
                                LPVOID lpBuffer, 
                                DWORD nNumberOfBytesToRead, 
                                LPDWORD lpNumberOfBytesRead, 
                                LPOVERLAPPED lpOverlapped);
BOOL WINAPI 
proxyBASS_ChannelPlay(          DWORD handle, 
                                BOOL restart);
BOOL WINAPI 
proxyBASS_ChannelSetPosition(   DWORD handle, 
                                QWORD pos, 
                                DWORD mode);
BOOL WINAPI 
proxyBASS_ChannelSetAttribute(  DWORD handle, 
                                DWORD attrib, 
                                float value);
BOOL WINAPI 
proxyBASS_ChannelPause(         DWORD handle);

#define MAKE_PROXY_DEF(module, function, proxy)             \
    HookEngine<decltype(proxy)> proxy##__hk =               \
    HookEngine<decltype(proxy)>(module, function, proxy)
#define SelectProxy(proxy_class) proxy_class##__hk
#define EngineHook(proxy_class)                             \
    SelectProxy(proxy_class).TryHook()
#define EngineUnhook(proxy_class)                           \
    SelectProxy(proxy_class).Unhook()
#define BeginHook()                                         \
    do                                                      \
    {                                                       \
        DetourTransactionBegin();                           \
        DetourUpdateThread(GetCurrentThread());             \
    } while(0)
#define EndHook() DetourTransactionCommit()

#define NAME_BASS_DLL                   L"bass.dll"
#define NAME_KERNEL_DLL                 L"kernel32.dll"
#define NAME_NAMED_PIPE                 L"\\\\.\\pipe\\osu!Lyrics"
#define AUDIO_FILE_INFO_TOKEN           "AudioFilename:"
#define MAX_MESSAGE_LENGTH              600

template <class funcType>
class HookEngine
{
private:
    funcType* proxy;
    bool isHooked;

public:
    funcType* OriginalFunction;

    HookEngine(const wchar_t *nameModule, const char *nameFunction, funcType* proxy)
    {
        this->OriginalFunction = (funcType*)GetProcAddress(GetModuleHandle(nameModule), nameFunction);
        this->proxy = proxy;
    }
    void TryHook()
    {
        if (!isHooked) this->isHooked = !!DetourAttach(&(PVOID&)OriginalFunction, (PVOID)proxy);
    }
    void Unhook()
    {
        if (isHooked) this->isHooked = !DetourDetach(&(PVOID&)OriginalFunction, (PVOID)proxy);
    }
};

class NamedPipe
{
    HANDLE                                      m_hEvent;
    HANDLE                                      m_hPipe;

    std::atomic<bool>                           m_isThreadRunning;
    std::atomic<bool>                           m_isPipeConnected;

    std::thread*                                m_ThreadObject;
    concurrency::concurrent_queue<std::wstring> m_ThreadQueues;

public:


    void Start(const std::wstring&& nPipe)
    {

        m_hEvent = CreateEvent(NULL, FALSE, FALSE, NULL);
        m_hPipe  = CreateNamedPipeW(

            nPipe.c_str(), 

            PIPE_ACCESS_OUTBOUND, PIPE_TYPE_MESSAGE | PIPE_WAIT, 1, MAX_MESSAGE_LENGTH, 0, INFINITE, NULL);

        m_isThreadRunning = true;
        
        m_ThreadObject = new std::thread([this]() {
            std::wstring wMessage;
            DWORD        nWritten;

            while (m_isThreadRunning)
            {
                //
                // ConnectNamedPipe는 클라이언트와 연결될 때까지 무한 대기함:
                // 취소는 DisconnectNamedPipe로 가능
                //
                if (ConnectNamedPipe(m_hPipe, NULL) || GetLastError() == ERROR_PIPE_CONNECTED)
                {
                    m_isPipeConnected = true;

                    // 메시지 큐가 비었을 때 최대 3초간 기다리고 다시 시도:
                    // 클라이언트 접속을 대기해야 하기 때문에 INTINITE 지양
                    if (!m_ThreadQueues.try_pop(wMessage))
                    {
                        WaitForSingleObject(m_hEvent, 3000);
                        continue;
                    }

                    if (WriteFile(m_hPipe, wMessage.c_str(), wMessage.length() * sizeof(std::wstring::value_type), &nWritten, NULL))
                    {
                        continue;
                    }
                }

                // ConnectedNamedPipe 또는 WriteFile 실패 시
                // 이전 client가 연결을 끊은 것이므로 handle 정리
                m_isPipeConnected = false;
                DisconnectNamedPipe(m_hPipe);
            }

            // 클라이언트 연결 종료
            m_isPipeConnected = false;
            DisconnectNamedPipe(m_hPipe);
            CloseHandle(m_hPipe);
        });

        return;
    }

    void Stop()
    {
        m_isThreadRunning = false;
        // 무한 대기 중인 ConnectNamedPipe 취소
        DisconnectNamedPipe(m_hPipe);
        m_ThreadObject->join();
        delete m_ThreadObject;

        CloseHandle(m_hEvent);
    }

    void PushMessage(const std::wstring&& message)
    {
        if (!(m_isPipeConnected & m_isThreadRunning))
        {
            return;
        }

        m_ThreadQueues.push(message);
        SetEvent(m_hEvent);
    }
};