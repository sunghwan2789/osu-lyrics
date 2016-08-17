#include "Server.h"

#include <string>

#include <Windows.h>

DWORD WINAPI Server::Run(LPVOID lParam)
{
    this->hPushEvent = CreateEvent(NULL, FALSE, FALSE, NULL);

    this->hPipe = CreateNamedPipe(L"\\\\.\\pipe\\osu!Lyrics", PIPE_ACCESS_OUTBOUND,
        PIPE_TYPE_MESSAGE | PIPE_WAIT, 1, Server::nBufferSize, 0, INFINITE, NULL);
    std::wstring message;
    DWORD nNumberOfBytesWritten;
    // 스레드 종료 요청이 들어올 때까지 클라이언트 접속 무한 대기
    while (!this->isCancellationRequested)
    {
        // ConnectNamedPipe는 클라이언트와 연결될 때까지 무한 대기함:
        // 취소는 DisconnectNamedPipe로 가능
        if (ConnectNamedPipe(this->hPipe, NULL) || GetLastError() == ERROR_PIPE_CONNECTED)
        {
            this->isPipeConnected = true;

            // 메시지 큐가 비었을 때 최대 3초간 기다리고 다시 시도:
            // 클라이언트 접속을 대기해야 하기 때문에 INTINITE 지양
            if (!this->messageQueue.try_pop(message))
            {
                WaitForSingleObject(this->hPushEvent, 3000);
                continue;
            }

            if (WriteFile(this->hPipe, message.c_str(), message.length() * sizeof(std::wstring::value_type), &nNumberOfBytesWritten, NULL))
            {
                continue;
            }
        }
        this->isPipeConnected = false;
        DisconnectNamedPipe(this->hPipe);
    }
    // 클라이언트 연결 종료
    this->isPipeConnected = false;
    DisconnectNamedPipe(this->hPipe);
    CloseHandle(this->hPipe);

    CloseHandle(this->hPushEvent);
    return 0;
}

namespace bettertrunkneeded_maybetemplatetrunk_question
{
    DWORD WINAPI trunk(LPVOID lParam)
    {
        Server *server = (Server *) lParam;
        return server->Run(nullptr);
    }
}

void Server::Start()
{
    this->hThread = CreateThread(NULL, 0, bettertrunkneeded_maybetemplatetrunk_question::trunk, this, 0, NULL);
}

void Server::Stop()
{
    this->isCancellationRequested = true;
    DisconnectNamedPipe(this->hPipe);
    WaitForSingleObject(this->hThread, INFINITE);
    CloseHandle(this->hThread);
}

void Server::Update(std::wstring&& message)
{
    if (!this->isPipeConnected)
    {
        return;
    }

    this->messageQueue.push(message);
    SetEvent(this->hPushEvent);
}
