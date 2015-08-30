#pragma warning (disable:4996)

#define WIN32_LEAN_AND_MEAN
#pragma comment (lib, "Shlwapi.lib")

#include <Windows.h>
#include <Shlwapi.h>
#include <unordered_map>
#include <mutex>
#include <queue>
using namespace std;

#define BUF_SIZE MAX_PATH * 3

long long CurrentTime()
{
    long long t;
    GetSystemTimeAsFileTime((LPFILETIME) &t);
    return t;
}

BOOL unhook_by_code(LPCTSTR szDllName, LPCTSTR szFuncName, PBYTE pOrgBytes)
{
    FARPROC pFunc;
    DWORD dwOldProtect;

    // API 주소 구한다
    pFunc = GetProcAddress(GetModuleHandle(szDllName), szFuncName);

    // 원래 코드 (5 byte)를 덮어쓰기 위해 메모리에 WRITE 속성 추가
    VirtualProtect((LPVOID) pFunc, 5, PAGE_EXECUTE_READWRITE, &dwOldProtect);

    // Unhook
    memcpy(pFunc, pOrgBytes, 5);

    // 메모리 속성 복원
    VirtualProtect((LPVOID) pFunc, 5, dwOldProtect, &dwOldProtect);

    return TRUE;
}

BOOL hook_by_code(LPCTSTR szDllName, LPCTSTR szFuncName, PROC pfnNew, PBYTE pOrgBytes)
{
    FARPROC pfnOrg;
    DWORD dwOldProtect, dwAddress;
    BYTE pBuf[5] = { 0xE9, 0, };
    PBYTE pByte;

    // 후킹대상 API 주소를 구한다
    pfnOrg = (FARPROC) GetProcAddress(GetModuleHandle(szDllName), szFuncName);
    pByte = (PBYTE) pfnOrg;

    // 만약 이미 후킹되어 있다면 return FALSE
    if (pByte[0] == 0xE9)
        return FALSE;

    // 5 byte 패치를 위하여 메모리에 WRITE 속성 추가
    VirtualProtect((LPVOID) pfnOrg, 5, PAGE_EXECUTE_READWRITE, &dwOldProtect);

    // 기존코드 (5 byte) 백업
    memcpy(pOrgBytes, pfnOrg, 5);

    // JMP 주소계산 (E9 XXXX)
    // => XXXX = pfnNew - pfnOrg - 5
    dwAddress = (DWORD) pfnNew - (DWORD) pfnOrg - 5;
    memcpy(&pBuf[1], &dwAddress, 4);

    // Hook - 5 byte 패치(JMP XXXX)
    memcpy(pfnOrg, pBuf, 5);

    // 메모리 속성 복원
    VirtualProtect((LPVOID) pfnOrg, 5, dwOldProtect, &dwOldProtect);

    return TRUE;
}

mutex STLMutex;


HANDLE hPipe;
volatile bool bCancelPipeThread;
volatile bool bPipeConnected;

queue<string> MessageQueue;
HANDLE hQueuePushed;

DWORD WINAPI PipeThread(LPVOID lParam)
{
    hPipe = CreateNamedPipeA("\\\\.\\pipe\\osu!Lyrics", PIPE_ACCESS_OUTBOUND,
        PIPE_TYPE_MESSAGE | PIPE_WAIT, 1, BUF_SIZE * 5, 0, INFINITE, NULL);
    // 스레드 종료 요청이 들어올 때까지 클라이언트 접속 무한 대기
    while (!bCancelPipeThread)
    {
        // ConnectNamedPipe는 클라이언트와 연결될 때까지 무한 대기함:
        // 취소는 DisconnectNamedPipe로 가능
        if (ConnectNamedPipe(hPipe, NULL) || GetLastError() == ERROR_PIPE_CONNECTED)
        {
            bPipeConnected = true;

            STLMutex.lock();
            bool empty = MessageQueue.empty();
            STLMutex.unlock();
            if (empty)
            {
                // 메세지 큐가 비었을 때 3초간 기다려도 신호가 없으면 다시 기다림
                WaitForSingleObject(hQueuePushed, 3000);
                continue;
            }

            STLMutex.lock();
            string message = MessageQueue.front();
            STLMutex.unlock();
            OVERLAPPED overlapped = {};
            if (WriteFileEx(hPipe, message.c_str(), message.length(), &overlapped, [](DWORD, DWORD, LPOVERLAPPED) {}))
            {
                STLMutex.lock();
                MessageQueue.pop();
                STLMutex.unlock();
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


typedef BOOL (WINAPI *tReadFile)(HANDLE, LPVOID, DWORD, LPDWORD, LPOVERLAPPED);
tReadFile pReadFile;
mutex HookMutex;
BYTE pReadFileHook[5];

unordered_map<string, string> AudioInfo;

// osu!에서 ReadFile을 호출하면 정보를 빼내서 osu!Lyrics로 보냄
BOOL WINAPI hkReadFile(HANDLE hFile, LPVOID lpBuffer, DWORD nNumberOfBytesToRead, LPDWORD lpNumberOfBytesRead, LPOVERLAPPED lpOverlapped)
{
    long long calledAt = CurrentTime();

    HookMutex.lock();
    unhook_by_code("kernel32.dll", "ReadFile", pReadFileHook);
    BOOL result = pReadFile(hFile, lpBuffer, nNumberOfBytesToRead, lpNumberOfBytesRead, lpOverlapped);
    hook_by_code("kernel32.dll", "ReadFile", (PROC) hkReadFile, pReadFileHook);
    HookMutex.unlock();
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

                STLMutex.lock();
                AudioInfo.insert(make_pair(string(audioPath), string(path)));
                STLMutex.unlock();

                free(beatmapDir);
                break;
            }
            line = strtok(NULL, "\n");
        }
        free(buffer);
    }
    else
    {
        STLMutex.lock();
        // [ audioPath, beatmapPath ]
        unordered_map<string, string>::iterator pair = AudioInfo.find(string(path));
        bool found = pair != AudioInfo.end();
        STLMutex.unlock();
        if (found)
        {
            char message[BUF_SIZE];
            sprintf(message, "%llx|%s|%lx|%s\n", calledAt, &path[4], seekPosition, &pair->second[4]);
            STLMutex.lock();
            MessageQueue.push(string(message));
            STLMutex.unlock();
            SetEvent(hQueuePushed);
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
        hQueuePushed = CreateEventA(NULL, TRUE, FALSE, NULL);

        HookMutex.lock();
        pReadFile = (tReadFile) GetProcAddress(GetModuleHandle("kernel32.dll"), "ReadFile");
        hook_by_code("kernel32.dll", "ReadFile", (PROC) hkReadFile, pReadFileHook);
        HookMutex.unlock();
    }
    else if (fdwReason == DLL_PROCESS_DETACH)
    {
        HookMutex.lock();
        unhook_by_code("kernel32.dll", "ReadFile", pReadFileHook);
        HookMutex.unlock();

        bCancelPipeThread = true;
        DisconnectNamedPipe(hPipe);
        WaitForSingleObject(hPipeThread, INFINITE);
        CloseHandle(hQueuePushed);
        CloseHandle(hPipeThread);
    }
    return TRUE;
}