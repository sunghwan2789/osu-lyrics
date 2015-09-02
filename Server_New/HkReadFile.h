#pragma once

typedef BOOL(WINAPI *tReadFile)(HANDLE, LPVOID, DWORD, LPDWORD, LPOVERLAPPED);

class HkReadFile : public HookBase
{
private:
    char funcName[9] = "ReadFile";
    char moduleName[13] = "kernel32.dll";

    BOOL WINAPI lReadFile(
        HANDLE hFile, 
        LPVOID lpBuffer, 
        DWORD nNumberOfBytesToRead, 
        LPDWORD lpNumberOfBytesRead, 
        LPOVERLAPPED lpOverlapped);
public:
    void Hook();
    void UnHook();

};