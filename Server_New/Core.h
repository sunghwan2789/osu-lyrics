#pragma once


#include <mutex>
#include <list>

#include "winapi.h"
#include "Base.h"
#include "Heap.h"
#include "Hook.h"

typedef BOOL(WINAPI tReadFile)(HANDLE, LPVOID, DWORD, LPDWORD, LPOVERLAPPED);
typedef VOID(WINAPI tPostQuitMessage)(int);


class Core : public Base
{
private:
    Heap *pHeap;

    HookBase<tReadFile*> *ReadFileHook;
    HookBase<tPostQuitMessage*> *PostQuitMessageHook;

public:
    void Init();
    void Release();
};