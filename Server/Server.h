#pragma once

#include <string>
#include <atomic>
#include <concurrent_queue.h>

#include <Windows.h>
#include "Observer.h"

class Server : public Observer
{
public:
    static const DWORD nMessageLength = MAX_PATH * 3;
    static const DWORD nBufferSize = Server::nMessageLength * sizeof(std::wstring::value_type);

    Server() :
        hThread(NULL),
        isCancellationRequested(false),
        hPipe(NULL),
        isPipeConnected(false),
        hPushEvent(NULL) {}

    DWORD WINAPI Run(LPVOID);
    void Start();
    void Stop();

    virtual void Update(std::wstring&&);

private:
    HANDLE hThread;
    std::atomic<bool> isCancellationRequested;

    HANDLE hPipe;
    std::atomic<bool> isPipeConnected;

    concurrency::concurrent_queue<std::wstring> messageQueue;
    HANDLE hPushEvent;
};
