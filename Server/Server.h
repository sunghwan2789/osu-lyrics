#pragma once

#include <tchar.h>
#include <string>
#include <memory>
#include <mutex>
#include <atomic>
#include <concurrent_queue.h>

#include <Windows.h>

class Server
{
public:
    static const DWORD MAX_MESSAGE_LENGTH = MAX_PATH * 3;
    static const DWORD nBufferSize = Server::MAX_MESSAGE_LENGTH * sizeof(std::wstring::value_type);
    
    void Run();
    void Stop();
    void PushMessage(std::wstring&&);

    static DWORD WINAPI Thread(LPVOID);

    HANDLE hThread;
    std::atomic<bool> isThreadCanceled;

    HANDLE hPipe;
    std::atomic<bool> isPipeConnected;

    concurrency::concurrent_queue<std::wstring> messageQueue;
    HANDLE hPushEvent;
    Server() : 
        hThread(NULL), 
        isThreadCanceled(false), 
        hPipe(NULL), 
        isPipeConnected(false), 
        hPushEvent(NULL) {}
    ~Server() {}

    Server(const Server&) = delete;
    Server(Server&&) = delete;
    Server& operator=(const Server&) = delete;
    Server& operator=(Server&&) = delete;
};

static Server InstanceServer;
