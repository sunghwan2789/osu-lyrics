#pragma once

#include "Runnable.h"

#include <Windows.h>
#include "THUNK.h"

class Thread : public Runnable
{
public:
    Thread();
    ~Thread();

    void Start();
    void Wait();

private:
    DWORD Proc(LPVOID);

    THUNK* pThunk;
    HANDLE hThread;
};
