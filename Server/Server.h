#pragma once

#include "Observer.h"
#include "Thread.h"

#include <atomic>
#include <concurrent_queue.h>

#include <Windows.h>

class Server : public Observer, public Thread
{
public:
    static const DWORD nMessageLength = MAX_PATH * 3;
    static const DWORD nBufferSize = Server::nMessageLength * sizeof(std::wstring::value_type);

    Server();

    virtual void Run() override;
    void Stop();

    virtual void Update(const std::wstring&) override;

private:
    std::atomic<bool> isCancellationRequested;

    HANDLE hPipe;
    std::atomic<bool> isPipeConnected;

    concurrency::concurrent_queue<std::wstring> messageQueue;
    HANDLE hPushEvent;
};
