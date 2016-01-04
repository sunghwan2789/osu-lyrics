#pragma once

#include <tchar.h>

#include <Windows.h>

template<typename T>
class Hooker
{
public:
    Hooker(const TCHAR *, const char *, T *);
    ~Hooker();

    T *GetFunction();
    void SetHookFunction(T *);

    void Hook();
    void Unhook();

private:
    T *pFunction;
    T *pHookFunction;
    bool hooked;
};
