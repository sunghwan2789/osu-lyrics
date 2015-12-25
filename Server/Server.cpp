#include <Windows.h>
#include <string>
#include <concurrent_queue.h>
#include "Observer.h"
#include "Server.h"

concurrency::concurrent_queue<std::string> MessageQueue;
HANDLE hPushEvent;

HANDLE hServerThread;
HANDLE hPipe;
volatile bool bCancelServerThread;
volatile bool bPipeConnected;
DWORD WINAPI ServerThread(LPVOID lParam)
{
    hPipe = CreateNamedPipe("\\\\.\\pipe\\osu!Lyrics", PIPE_ACCESS_OUTBOUND,
        PIPE_TYPE_MESSAGE | PIPE_WAIT, 1, BUF_SIZE, 0, INFINITE, NULL);
    std::string message;
    DWORD wrote;
    // 스레드 종료 요청이 들어올 때까지 클라이언트 접속 무한 대기
    while (!bCancelServerThread)
    {
        // ConnectNamedPipe는 클라이언트와 연결될 때까지 무한 대기함:
        // 취소는 DisconnectNamedPipe로 가능
        if (ConnectNamedPipe(hPipe, NULL) || GetLastError() == ERROR_PIPE_CONNECTED)
        {
            bPipeConnected = true;

            // 메시지 큐가 비었을 때 최대 3초간 기다리고 다시 시도:
            // 클라이언트 접속을 대기해야 하기 때문에 INTINITE 지양
            if (!MessageQueue.try_pop(message))
            {
                WaitForSingleObject(hPushEvent, 3000);
                continue;
            }

            if (WriteFile(hPipe, message.c_str(), message.length(), &wrote, NULL))
            {
                continue;
            }
        }
        bPipeConnected = false;
        DisconnectNamedPipe(hPipe);
    }
    // 클라이언트 연결 종료
    bPipeConnected = false;
    DisconnectNamedPipe(hPipe);
    CloseHandle(hPipe);
    return 0;
}

void PushMessage(std::string &&message)
{
    if (!bPipeConnected)
    {
        return;
    }

    MessageQueue.push(message);
    SetEvent(hPushEvent);
}

BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved)
{
    if (fdwReason == DLL_PROCESS_ATTACH)
    {
        hPushEvent = CreateEvent(NULL, FALSE, FALSE, NULL);

        hServerThread = CreateThread(NULL, 0, ServerThread, NULL, 0, NULL);

        RunObserver();
    }
    else if (fdwReason == DLL_PROCESS_DETACH)
    {
        StopObserver();

        bCancelServerThread = true;
        DisconnectNamedPipe(hPipe);
        WaitForSingleObject(hServerThread, INFINITE);
        CloseHandle(hServerThread);

        CloseHandle(hPushEvent);
    }
    return TRUE;
}
