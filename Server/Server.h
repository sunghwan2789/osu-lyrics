#pragma once

#include <tchar.h>
#include <string>
#include <memory>
#include <mutex>
#include <concurrent_queue.h>

#include <Windows.h>

typedef std::basic_string<TCHAR> tstring;

class Server
{
public:
    static const DWORD nMessageLength = MAX_PATH * 3;
    static const DWORD nBufferSize = Server::nMessageLength * sizeof(tstring::value_type);
    
    void Run();
    void Stop();
    void PushMessage(tstring&&);

private:
    static DWORD WINAPI Thread(LPVOID);

    HANDLE hThread;
    std::atomic<bool> cancelThread;

    HANDLE hPipe;
    std::atomic<bool> pipeConnected;

    concurrency::concurrent_queue<tstring> messageQueue;
    HANDLE hPushEvent;

public:
    static Server *GetInstance()
    {
        std::call_once(Server::once_flag, []
        {
            Server::instance.reset(new Server, [](Server *p)
            {
                delete p;
            });
        });
        return Server::instance.get();
    }

private:
    Server() : hThread(NULL), cancelThread(false), hPipe(NULL), pipeConnected(false), hPushEvent(NULL) {}
    ~Server() {}

    Server(const Server&) = delete;
    Server(Server&&) = delete;
    Server& operator=(const Server&) = delete;
    Server& operator=(Server&&) = delete;

    static std::shared_ptr<Server> instance;
    static std::once_flag once_flag;
};
