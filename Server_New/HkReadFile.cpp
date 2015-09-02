#include "Core.h"

void HkReadFile::Hook()
{
    DWORD dwOldProtect = NULL;
    DWORD dwAddress = NULL;

    this->dwFuncRefAddr = (DWORD)GetProcAddress(GetModuleHandleA(this->moduleName), this->funcName);

    VirtualProtect(LPVOID(this->dwFuncRefAddr), szOpcode, PAGE_EXECUTE_READWRITE, &dwOldProtect);

    memcpy(this->pFuncRef->Object(), LPVOID(this->dwFuncRefAddr), szOpcode);
    dwAddress = DWORD(this->lReadFile) - DWORD(this->dwFuncRefAddr) - szOpcode;

    memcpy(LPVOID(this->dwFuncRefAddr), &asmJmp, sizeof(BYTE));
    memcpy(LPVOID(this->dwFuncRefAddr + 1), (LPVOID)dwAddress, sizeof(DWORD));

    VirtualProtect(LPVOID(this->dwFuncRefAddr), 5, dwOldProtect, nullptr);

    return;
}

void HkReadFile::UnHook()
{
    DWORD dwOldProtect = NULL;

    VirtualProtect(LPVOID(this->dwFuncRefAddr), szOpcode, PAGE_EXECUTE_READWRITE, &dwOldProtect);

    memcpy(LPVOID(this->dwFuncRefAddr), this->pFuncRef->Object(), szOpcode);

    VirtualProtect(LPVOID(this->dwFuncRefAddr), szOpcode, dwOldProtect, nullptr);

    return;
}

BOOL WINAPI HkReadFile::lReadFile(
    HANDLE hFile,
    LPVOID lpBuffer,
    DWORD nNumberOfBytesToRead,
    LPDWORD lpNumberOfBytesRead,
    LPOVERLAPPED lpOverlapped)
{
    BOOL result;

    result = this->pFuncRef->Run<tReadFile>()(
        hFile,
        lpBuffer, 
        nNumberOfBytesToRead, 
        lpNumberOfBytesRead, 
        lpOverlapped);
}