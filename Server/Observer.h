#pragma once

#include <Windows.h>
#include "bass.h"

typedef BOOL (WINAPI *tReadFile)(HANDLE, LPVOID, DWORD, LPDWORD, LPOVERLAPPED);
typedef BOOL (BASSDEF(*tBASS_ChannelPlay))(DWORD, BOOL);
typedef BOOL (BASSDEF(*tBASS_ChannelSetPosition))(DWORD, QWORD, DWORD);
typedef BOOL (BASSDEF(*tBASS_ChannelSetAttribute))(DWORD, DWORD, float);
typedef BOOL (BASSDEF(*tBASS_ChannelPause))(DWORD);
BOOL WINAPI hkReadFile(HANDLE, LPVOID, DWORD, LPDWORD, LPOVERLAPPED);
BOOL BASSDEF(hkBASS_ChannelPlay)(DWORD, BOOL);
BOOL BASSDEF(hkBASS_ChannelSetPosition)(DWORD, QWORD, DWORD);
BOOL BASSDEF(hkBASS_ChannelSetAttribute)(DWORD, DWORD, float);
BOOL BASSDEF(hkBASS_ChannelPause)(DWORD);
void cbBASS_Control(long long, double, float);

void RunObserver();
void StopObserver();
