#define WIN32_LEAN_AND_MEAN
#pragma comment (lib, "Shlwapi.lib")

#pragma warning (disable:4996)

#include <Windows.h>
#include <Shlwapi.h>
#include <unordered_map>
#include <mutex>
#include <queue>

using namespace std;

#define BUF_SIZE MAX_PATH*3

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


HANDLE hPipe;
char buffer[BUF_SIZE];
mutex pQueueMutex;

volatile BOOL bCancelPipeThread = FALSE;
volatile BOOL bIsPipeConnected = FALSE;

DWORD WINAPI PipeManager(LPVOID lParam)
{
    hPipe = CreateNamedPipe("\\\\.\\pipe\\osu!Lyrics", PIPE_ACCESS_OUTBOUND,
        PIPE_TYPE_MESSAGE | PIPE_WAIT, 1, BUF_SIZE * 5, 0, INFINITE, NULL);
    // 클라이언트가 연결할 때까지 무한 대기
    while (!bCancelPipeThread)
    {
        if ((bIsPipeConnected = ConnectNamedPipe(hPipe, NULL)) || GetLastError() == ERROR_PIPE_CONNECTED)
        {
			pQueueMutex.lock(); //큐 뮤텍스가 언록될때까지 기다린다. 언록되면 록.
			string message = buffer;
			pQueueMutex.unlock(); //언록되면 buffer를 메세지로 카피한 수에 다시 언록한다.
            OVERLAPPED overlapped = {};
            if (WriteFileEx(hPipe, message.c_str(), message.length(), &overlapped, [](DWORD, DWORD, LPOVERLAPPED) {}))
            {
                continue;
            }
        }
		bIsPipeConnected = FALSE;
        DisconnectNamedPipe(hPipe);
    }
    // 클라이언트 연결 종료
    return 0;
}


typedef BOOL (WINAPI *tReadFile)(HANDLE, LPVOID, DWORD, LPDWORD, LPOVERLAPPED);
tReadFile pReadFile;
BYTE pReadFileJMP[5];

mutex pBinaryMutex;

unordered_map<string, string> audioInfo;

// osu!에서 ReadFile을 호출하면 정보를 빼내서 osu!Lyrics로 보냄
BOOL WINAPI hkReadFile(HANDLE hFile, LPVOID lpBuffer, DWORD nNumberOfBytesToRead, LPDWORD lpNumberOfBytesRead, LPOVERLAPPED lpOverlapped)
{
    long long calledAt = CurrentTime();
    BOOL result = FALSE;

	pBinaryMutex.lock();
    {
        unhook_by_code("kernel32.dll", "ReadFile", pReadFileJMP);
        result = pReadFile(hFile, lpBuffer, nNumberOfBytesToRead, lpNumberOfBytesRead, lpOverlapped);
        hook_by_code("kernel32.dll", "ReadFile", (PROC) hkReadFile, pReadFileJMP);
    }
	pBinaryMutex.unlock();
    if (!result || !bIsPipeConnected)
    {
        // result가 실패하거나 파이프가 커넥트되어있지 않을경우엔 바로 함수를 리턴한다.
        return result;
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

                audioInfo.insert(make_pair(string(audioPath), string(path)));
                
                free(beatmapDir);
                break;
            }
            line = strtok(NULL, "\n");
        }
        free(buffer);
    }
    else
    {
        // [ audioPath, beatmapPath ]
        unordered_map<string, string>::iterator it = audioInfo.find(string(path));
        if (it != audioInfo.end())
        {
            char TmpBuffer[BUF_SIZE];
            sprintf(buffer, "%llx|%s|%lx|%s\n", calledAt, &path[4], seekPosition, &it->second[4]);
			pQueueMutex.unlock(); //버퍼에 메세지를 쓰고나서 언록한다. 그러면 윗부분의 파이프에서 lock된다.
			pQueueMutex.lock(); 
			/*파이프에서 lock되고 이쪽의 뮤텍스는 다시 록 큐를 기다린다. 그레서 파이프가 언록된 직후에 다시 록된다.
			  그레서 리피터(펄서) 같이 처리가 이루어진다. 이쪽이 다시 록되면 이쪽이 다시 메세지를 받아서 언록할때
			  까지 파이프에서는 lock함수로 기다리게 된다.*/
        }
    }
    return TRUE;
}


HANDLE hPipeThread;

BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved)
{
    if (fdwReason == DLL_PROCESS_ATTACH)
    {
        hPipeThread = CreateThread(NULL, 0, PipeManager, NULL, 0, NULL);
        
		pBinaryMutex.lock();
        {
            pReadFile = (tReadFile) GetProcAddress(GetModuleHandle("kernel32.dll"), "ReadFile");
            hook_by_code("kernel32.dll", "ReadFile", (PROC) hkReadFile, pReadFileJMP);
        }
		pBinaryMutex.unlock();
    }
    else if (fdwReason == DLL_PROCESS_DETACH)
    {
        bCancelPipeThread = TRUE;

        WaitForSingleObject(hPipeThread, INFINITE);

		DisconnectNamedPipe(hPipe);

        CloseHandle(hPipeThread);
		CloseHandle(hPipe);

		pBinaryMutex.lock();
        {
            unhook_by_code("kernel32.dll", "ReadFile", pReadFileJMP);
        }
		pBinaryMutex.unlock();
    }
    return TRUE;
}