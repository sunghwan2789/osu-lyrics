#include "Server.h"

#include "Observer.h"

DWORD WINAPI Server::Thread(LPVOID lParam)
{
    InstanceServer.hPipe = CreateNamedPipe(L"\\\\.\\pipe\\osu!Lyrics", PIPE_ACCESS_OUTBOUND,
        PIPE_TYPE_MESSAGE | PIPE_WAIT, 1, Server::nBufferSize, 0, INFINITE, NULL);

    std::wstring message;
    DWORD nNumberOfBytesWritten;
    // 스레드 종료 요청이 들어올 때까지 클라이언트 접속 무한 대기
    while (!InstanceServer.isThreadCanceled)
    {
        // ConnectNamedPipe는 클라이언트와 연결될 때까지 무한 대기함:
        // 취소는 DisconnectNamedPipe로 가능
        if (ConnectNamedPipe(InstanceServer.hPipe, NULL) || GetLastError() == ERROR_PIPE_CONNECTED)
        {
            InstanceServer.isPipeConnected = true;

            // 메시지 큐가 비었을 때 최대 3초간 기다리고 다시 시도:
            // 클라이언트 접속을 대기해야 하기 때문에 INTINITE 지양
            if (!InstanceServer.messageQueue.try_pop(message))
            {
                WaitForSingleObject(InstanceServer.hPushEvent, 3000);
                continue;
            }

            if (WriteFile(InstanceServer.hPipe, message.c_str(), message.length() * sizeof(std::wstring::value_type), &nNumberOfBytesWritten, NULL))
            {
                continue;
            }
        }
        InstanceServer.isPipeConnected = false;
        DisconnectNamedPipe(InstanceServer.hPipe);
    }
    // 클라이언트 연결 종료
    InstanceServer.isPipeConnected = false;
    DisconnectNamedPipe(InstanceServer.hPipe);
    CloseHandle(InstanceServer.hPipe);
    return 0;
}

void Server::PushMessage(std::wstring&& message)
{
    if (!this->isPipeConnected)
    {
        return;
    }

    this->messageQueue.push(message);
    SetEvent(this->hPushEvent);
}

void Server::Run()
{
    this->hPushEvent = CreateEvent(NULL, FALSE, FALSE, NULL);

    this->hThread = CreateThread(NULL, 0, Server::Thread, NULL, 0, NULL);
}

void Server::Stop()
{
    this->isThreadCanceled = true;
    DisconnectNamedPipe(this->hPipe);
    WaitForSingleObject(this->hThread, INFINITE);
    CloseHandle(this->hThread);

    CloseHandle(this->hPushEvent);
}

BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved)
{
    if (fdwReason == DLL_PROCESS_ATTACH)
    {
        InstanceServer.Run();
        InstanceObserver.Start();
    }
    else if (fdwReason == DLL_PROCESS_DETACH)
    {
        InstanceObserver.Stop();
        InstanceServer.Stop();
    }
    return TRUE;
}
