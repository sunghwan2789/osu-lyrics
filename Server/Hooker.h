#pragma once

#include <tchar.h>

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

#include "Hooker.hpp"
