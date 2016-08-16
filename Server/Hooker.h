#pragma once

#include <cwchar>

template<typename T>
class Hooker
{
public:
    Hooker(const wchar_t *, const char *, T *);
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
