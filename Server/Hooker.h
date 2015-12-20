#pragma once

#include <Windows.h>
#include "bass.h"

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

    Hooker(const char *, const char *, F = nullptr);
    ~Hooker();

    void Set(F);

    void Hook();
    void Unhook();

    void EnterCS();
    void LeaveCS();
};

typedef BOOL (WINAPI *tReadFile)(HANDLE, LPVOID, DWORD, LPDWORD, LPOVERLAPPED);
BOOL WINAPI hkReadFile(HANDLE, LPVOID, DWORD, LPDWORD, LPOVERLAPPED);
typedef BOOL (BASSDEF(*tBASS_ChannelPlay))(DWORD, BOOL);
BOOL BASSDEF(hkBASS_ChannelPlay)(DWORD, BOOL);
typedef BOOL (BASSDEF(*tBASS_ChannelSetPosition))(DWORD, QWORD, DWORD);
BOOL BASSDEF(hkBASS_ChannelSetPosition)(DWORD, QWORD, DWORD);
typedef BOOL (BASSDEF(*tBASS_ChannelSetAttribute))(DWORD, DWORD, float);
BOOL BASSDEF(hkBASS_ChannelSetAttribute)(DWORD, DWORD, float);
