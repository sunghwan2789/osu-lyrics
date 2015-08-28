#define WIN32_LEAN_AND_MEAN
#pragma comment (lib, "Shlwapi.lib")

#include <Windows.h>
#include <Shlwapi.h>
#include <unordered_map>
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


HANDLE hPipe;
BOOL bPipeConnected;

DWORD WINAPI PipeMan(LPVOID lParam)
{
    hPipe = CreateNamedPipe("\\\\.\\pipe\\osu!Lyrics", PIPE_ACCESS_OUTBOUND, PIPE_TYPE_MESSAGE | PIPE_WAIT, 1, BUF_SIZE * 5, 0, 1000, NULL);
    while (1)
    {
        if (ConnectNamedPipe(hPipe, NULL) || GetLastError() == ERROR_PIPE_CONNECTED)
        {
            bPipeConnected = TRUE;
        }
        else
        {
            bPipeConnected = FALSE;
            DisconnectNamedPipe(hPipe);
        }
        Sleep(1000);
    }
    DisconnectNamedPipe(hPipe);
    CloseHandle(hPipe);
    return 0;
}


typedef BOOL (WINAPI *tReadFile)(HANDLE, LPVOID, DWORD, LPDWORD, LPOVERLAPPED);
tReadFile oReadFile;
BYTE bReadFile[5] = {};
CRITICAL_SECTION lReadFile;
OVERLAPPED overlapped;

DWORD dirLen = 4;
unordered_map<string, string> audioMap;

BOOL WINAPI hkReadFile(HANDLE hFile, LPVOID lpBuffer, DWORD nNumberOfBytesToRead, LPDWORD lpNumberOfBytesRead, LPOVERLAPPED lpOverlapped)
{
    long long sReadFile = CurrentTime();
    EnterCriticalSection(&lReadFile);

    unhook_by_code("kernel32.dll", "ReadFile", bReadFile);
    BOOL rReadFile = oReadFile(hFile, lpBuffer, nNumberOfBytesToRead, lpNumberOfBytesRead, lpOverlapped);
    hook_by_code("kernel32.dll", "ReadFile", (PROC) hkReadFile, bReadFile);

    LeaveCriticalSection(&lReadFile);
    if (!rReadFile)
    {
        return FALSE;
    }

    char path[MAX_PATH];
    if (strnicmp(".osu", &path[GetFinalPathNameByHandle(hFile, path, MAX_PATH, VOLUME_NAME_DOS) - 4], 4) == 0 &&
        SetFilePointer(hFile, 0, NULL, FILE_CURRENT) == *lpNumberOfBytesRead)
    {
        char *buff = strdup((char *) lpBuffer);
        char *token = strtok(buff, "\n");
        while (token != NULL)
        {
            if (strnicmp(token, "AudioFilename:", 14) == 0)
            {
                char audio[MAX_PATH];

                int i = 14;
                for (; token[i] == ' '; i++);
                buff[0] = '\0';
                strncat(buff, &token[i], strlen(token) - i - 1);

                token = strdup(path);
                PathRemoveFileSpec(token);
                PathCombine(audio, token, buff);
                free(token);

                // 검색할 때 대소문자 구분하므로 제대로 된 파일명 얻기
                WIN32_FIND_DATA fdata;
                FindClose(FindFirstFile(audio, &fdata));
                PathRemoveFileSpec(audio);
                PathCombine(audio, audio, fdata.cFileName);

                string tmp(audio);
                if (audioMap.find(tmp) == audioMap.end())
                {
                    audioMap.insert(make_pair(tmp, string(&path[dirLen])));
                }
                break;
            }
            token = strtok(NULL, "\n");
        }
        free(buff);
    }
    else if (bPipeConnected)
    {
        auto pair = audioMap.find(string(path)); // [ audioPath, beatmapPath ]
        if (pair != audioMap.end())
        {
            char buff[BUF_SIZE];
            if (!WriteFileEx(hPipe, buff, sprintf(buff, "%llx|%s|%lx|%s\n", sReadFile, &path[dirLen], SetFilePointer(hFile, 0, NULL, FILE_CURRENT) - *lpNumberOfBytesRead, pair->second), &overlapped, [](DWORD, DWORD, LPOVERLAPPED) {}))
            {
                bPipeConnected = FALSE;
            }
        }
    }
    return TRUE;
}


BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved)
{
    if (fdwReason == DLL_PROCESS_ATTACH)
    {
        HANDLE hThread = NULL;
        hThread = CreateThread(NULL, 0, PipeMan, NULL, 0, NULL);
        WaitForSingleObject(hThread, 0xFFFFFF);
        CloseHandle(hThread);

        char dir[MAX_PATH];
        GetModuleFileName(NULL, dir, MAX_PATH);
        PathRemoveFileSpec(dir);
        dirLen = 4 + strlen(dir);

        InitializeCriticalSection(&lReadFile);
        oReadFile = (tReadFile) GetProcAddress(GetModuleHandle("kernel32.dll"), "ReadFile");
        hook_by_code("kernel32.dll", "ReadFile", (PROC) hkReadFile, bReadFile);
    }
    else if (fdwReason == DLL_PROCESS_DETACH)
    {
        EnterCriticalSection(&lReadFile);

        unhook_by_code("kernel32.dll", "ReadFile", bReadFile);

        LeaveCriticalSection(&lReadFile);
        DeleteCriticalSection(&lReadFile);
    }
    return TRUE;
}
