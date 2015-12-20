#pragma once

#include <Windows.h>

const BYTE szOpcode = 1 + sizeof(DWORD);
const BYTE asmJmp = 0xE9;

template<typename F> class Hooker
{
private:
    BYTE OriginalOpcode[szOpcode];
    CRITICAL_SECTION hMutex;
    F hkFunction;
    bool Hooked;

public:
    F pFunction;

    Hooker(const char *, const char *);
    ~Hooker();

    void Set(F);

    void Hook();
    void Unhook();

    void EnterCS();
    void LeaveCS();
};

typedef BOOL (WINAPI *tReadFile)(HANDLE, LPVOID, DWORD, LPDWORD, LPOVERLAPPED);
