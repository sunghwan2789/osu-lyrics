#pragma once

#include <tchar.h>
#include "detours.h"

template<typename TypeFunction>
class Hooker
{
public:
    Hooker(const TCHAR *, const char *, TypeFunction *);
    ~Hooker();

	TypeFunction *GetFunction();
    void SetHookFunction(TypeFunction *);

    void Hook();
    void Unhook();

private:
	TypeFunction *pFunction;
	TypeFunction *pHookFunction;
    bool isHooked;
};

#include "Hooker.hpp"
