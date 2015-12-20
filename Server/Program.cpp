#define WIN32_LEAN_AND_MEAN
#pragma comment (lib, "Shlwapi.lib")

#include <Windows.h>
#include <Shlwapi.h>
#include <unordered_map>
#include <mutex>
#include "ConcurrentQueue.h"
#include "Hooker.h"
using namespace std;

#define BUF_SIZE MAX_PATH * 3

long long CurrentTime()
{
    long long t;
    GetSystemTimeAsFileTime((LPFILETIME) &t);
    return t;
}


HANDLE hPipe;
volatile bool bCancelPipeThread;
volatile bool bPipeConnected;

ConcurrentQueue<string> MessageQueue;

DWORD WINAPI PipeThread(LPVOID lParam)
{
    hPipe = CreateNamedPipe("\\\\.\\pipe\\osu!Lyrics", PIPE_ACCESS_OUTBOUND,
        PIPE_TYPE_MESSAGE | PIPE_WAIT, 1, BUF_SIZE, 0, INFINITE, NULL);
    // 스레드 종료 요청이 들어올 때까지 클라이언트 접속 무한 대기
    while (!bCancelPipeThread)
    {
        // ConnectNamedPipe는 클라이언트와 연결될 때까지 무한 대기함:
        // 취소는 DisconnectNamedPipe로 가능
        if (ConnectNamedPipe(hPipe, NULL) || GetLastError() == ERROR_PIPE_CONNECTED)
        {
            bPipeConnected = true;

            if (MessageQueue.Empty())
            {
                // 메세지 큐가 비었을 때 3초간 기다려도 신호가 없으면 다시 기다림
                MessageQueue.WaitPush(3000);
                continue;
            }

            DWORD wrote;
            string message = MessageQueue.Pop();
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


Hooker<tReadFile> hkrReadFile("kernel32.dll", "ReadFile");
unordered_map<string, string> AudioInfo;

// osu!에서 ReadFile을 호출하면 정보를 빼내서 osu!Lyrics로 보냄
BOOL WINAPI hkReadFile(HANDLE hFile, LPVOID lpBuffer, DWORD nNumberOfBytesToRead, LPDWORD lpNumberOfBytesRead, LPOVERLAPPED lpOverlapped)
{
    long long calledAt = CurrentTime();

    hkrReadFile.EnterCS();
    hkrReadFile.Unhook();
    BOOL result = hkrReadFile.pFunction(hFile, lpBuffer, nNumberOfBytesToRead, lpNumberOfBytesRead, lpOverlapped);
    hkrReadFile.Hook();
    hkrReadFile.LeaveCS();
    if (!result)
    {
        return FALSE;
    }

    char path[MAX_PATH];
    DWORD pathLength = GetFinalPathNameByHandle(hFile, path, MAX_PATH, VOLUME_NAME_DOS);
    //                  1: \\?\D:\Games\osu!\...
    DWORD seekPosition = SetFilePointer(hFile, 0, NULL, FILE_CURRENT) - *lpNumberOfBytesRead;
    // 지금 읽는 파일이 비트맵 파일이고 앞부분을 읽었다면:
    // AudioFilename은 앞부분에 있음 / 파일 핸들 또 열지 말고 일 한 번만 하자!
    if (strnicmp(".osu", &path[pathLength - 4], 4) == 0 && seekPosition == 0)
    {
        // strtok은 소스를 변형하므로 일단 백업
        char *buffer = strdup((char *) lpBuffer);
        char *line = strtok(buffer, "\n");
        while (line != NULL)
        {
            // 비트맵의 음악 파일 경로 얻기
            if (strnicmp(line, "AudioFilename:", 14) == 0)
            {
                char *beatmapDir = strdup(path);
                PathRemoveFileSpec(beatmapDir);

                char audioPath[MAX_PATH];

                // get value & trim
                int i = 14;
                for (; line[i] == ' '; i++);
                buffer[0] = '\0';
                strncat(buffer, &line[i], strlen(line) - i - 1);
                PathCombine(audioPath, beatmapDir, buffer);

                // 검색할 때 대소문자 구분하므로 제대로 된 파일 경로 얻기
                WIN32_FIND_DATA fdata;
                FindClose(FindFirstFile(audioPath, &fdata));
                PathRemoveFileSpec(audioPath);
                PathCombine(audioPath, audioPath, fdata.cFileName);

                AudioInfo.insert(make_pair(string(audioPath), string(path)));

                free(beatmapDir);
                break;
            }
            line = strtok(NULL, "\n");
        }
        free(buffer);
    }
    // 파이프 연결이 끊겼으면 유저가 가사를 보고 싶지 않다는 것:
    // osu!가 게임 플레이에만 집중하게 하자... 자원 낭비 금지
    else if (bPipeConnected)
    {
        // [ audioPath, beatmapPath ]
        unordered_map<string, string>::iterator pair = AudioInfo.find(string(path));
        if (pair != AudioInfo.end())
        {
            char message[BUF_SIZE];
            sprintf(message, "%llx|%s|%lx|%s\n", calledAt, &path[4], seekPosition, &pair->second[4]);
            MessageQueue.Push(string(message));
        }
    }
    return TRUE;
}


HANDLE hPipeThread;

BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved)
{
    if (fdwReason == DLL_PROCESS_ATTACH)
    {
        hPipeThread = CreateThread(NULL, 0, PipeThread, NULL, 0, NULL);

        hkrReadFile.Set(hkReadFile);
        hkrReadFile.Hook();
    }
    else if (fdwReason == DLL_PROCESS_DETACH)
    {
        // hkrReadFile.EnterCS();
        hkrReadFile.Unhook();
        // hkrReadFile.LeaveCS();

        bCancelPipeThread = true;
        DisconnectNamedPipe(hPipe);
        WaitForSingleObject(hPipeThread, INFINITE);
        CloseHandle(hPipeThread);
    }
    return TRUE;
}
