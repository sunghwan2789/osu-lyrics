#pragma once

#include <mutex>
#include <list>

#include "winapi.h"
#include "Heap.h"
#include "Base.h"
#include "Hook.h"

class Core : public Base
{
private:
    Heap *pHeap;

    HookBase *pHkReadFile;
    HookBase *pHkPostQuitMessage;

public:
    void Init();
    void Release();
};