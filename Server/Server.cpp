#include "Server.h"

#include "Observer.h"

std::shared_ptr<Server> Server::instance;
std::once_flag Server::once_flag;

DWORD WINAPI Server::Thread(LPVOID lParam)
{
    Server *self = Server::GetInstance();
    self->hPipe = CreateNamedPipe(_T("\\\\.\\pipe\\osu!Lyrics"), PIPE_ACCESS_OUTBOUND,
        PIPE_TYPE_MESSAGE | PIPE_WAIT, 1, Server::nBufferSize, 0, INFINITE, NULL);
    tstring message;
    DWORD nNumberOfBytesWritten;
    // 스레드 종료 요청이 들어올 때까지 클라이언트 접속 무한 대기
    while (!self->cancelThread)
    {
        // ConnectNamedPipe는 클라이언트와 연결될 때까지 무한 대기함:
        // 취소는 DisconnectNamedPipe로 가능
        if (ConnectNamedPipe(self->hPipe, NULL) || GetLastError() == ERROR_PIPE_CONNECTED)
        {
            self->pipeConnected = true;

            // 메시지 큐가 비었을 때 최대 3초간 기다리고 다시 시도:
            // 클라이언트 접속을 대기해야 하기 때문에 INTINITE 지양
            if (!self->messageQueue.try_pop(message))
            {
                WaitForSingleObject(self->hPushEvent, 3000);
                continue;
            }

            if (WriteFile(self->hPipe, message.c_str(), message.length() * sizeof(tstring::value_type), &nNumberOfBytesWritten, NULL))
            {
                continue;
            }
        }
        self->pipeConnected = false;
        DisconnectNamedPipe(self->hPipe);
    }
    // 클라이언트 연결 종료
    self->pipeConnected = false;
    DisconnectNamedPipe(self->hPipe);
    CloseHandle(self->hPipe);
    return 0;
}

void Server::PushMessage(tstring&& message)
{
    if (!this->pipeConnected)
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
    this->cancelThread = true;
    DisconnectNamedPipe(this->hPipe);
    WaitForSingleObject(this->hThread, INFINITE);
    CloseHandle(this->hThread);

    CloseHandle(this->hPushEvent);
}

BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved)
{
    if (fdwReason == DLL_PROCESS_ATTACH)
    {
        Server::GetInstance()->Run();
        Observer::GetInstance()->Run();
    }
    else if (fdwReason == DLL_PROCESS_DETACH)
    {
        Observer::GetInstance()->Stop();
        Server::GetInstance()->Stop();
    }
    return TRUE;
}
