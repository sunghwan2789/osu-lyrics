#pragma once

#include <cwchar>

template<typename TypeFunction>
class Hooker
{
public:
    Hooker(const wchar_t *, const char *, TypeFunction *);
    ~Hooker();

    TypeFunction *GetOriginalFunction();
    void SetHookFunction(TypeFunction *);

    void Hook();
    void Unhook();

private:
    TypeFunction *pFunction;
    TypeFunction *pHookFunction;
    bool isHooked;
};

#include "Hooker.hpp"
