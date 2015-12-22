#define WIN32_LEAN_AND_MEAN

#include <Windows.h>
#include <string>
#include "ConcurrentQueue.h"
#include "Observer.h"
#include "Server.h"

ConcurrentQueue<std::string> MessageQueue;

HANDLE hPipeThread;
HANDLE hPipe;
volatile bool bCancelPipeThread;
volatile bool bPipeConnected;
DWORD WINAPI PipeThread(LPVOID lParam)
{
    hPipe = CreateNamedPipe("\\\\.\\pipe\\osu!Lyrics", PIPE_ACCESS_OUTBOUND,
        PIPE_TYPE_MESSAGE | PIPE_WAIT, 1, BUF_SIZE, 0, INFINITE, NULL);
    std::string message;
    // 스레드 종료 요청이 들어올 때까지 클라이언트 접속 무한 대기
    while (!bCancelPipeThread)
    {
        // ConnectNamedPipe는 클라이언트와 연결될 때까지 무한 대기함:
        // 취소는 DisconnectNamedPipe로 가능
        BOOL initialized = ConnectNamedPipe(hPipe, NULL);
        if (initialized || GetLastError() == ERROR_PIPE_CONNECTED)
        {
            if (initialized)
            {
                // 프로그램 다시 시작할 때 이전 메시지 바로 전송
                MessageQueue.Push(message);
            }

            bPipeConnected = true;

            if (MessageQueue.Empty())
            {
                // 메세지 큐가 비었을 때 3초간 기다려도 신호가 없으면 다시 기다림
                MessageQueue.WaitPush(3000);
                continue;
            }

            DWORD wrote;
            message = MessageQueue.Pop();
            if (WriteFile(hPipe, message.c_str(), message.length(), &wrote, NULL))
            {
                continue;
            }
        }
        bPipeConnected = false;
        DisconnectNamedPipe(hPipe);

        MessageQueue.Clear();
    }
    // 클라이언트 연결 종료
    bPipeConnected = false;
    DisconnectNamedPipe(hPipe);
    CloseHandle(hPipe);
    return 0;
}

void PushMessage(const std::string &message)
{
    if (!bPipeConnected)
    {
        return;
    }

    MessageQueue.Push(message);
}

BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved)
{
    if (fdwReason == DLL_PROCESS_ATTACH)
    {
        hPipeThread = CreateThread(NULL, 0, PipeThread, NULL, 0, NULL);

        RunObserver();
    }
    else if (fdwReason == DLL_PROCESS_DETACH)
    {
        StopObserver();

        bCancelPipeThread = true;
        DisconnectNamedPipe(hPipe);
        WaitForSingleObject(hPipeThread, INFINITE);
        CloseHandle(hPipeThread);
    }
    return TRUE;
}
