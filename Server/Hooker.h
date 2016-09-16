#pragma once

#include <Windows.h>
#include "detours.h"
#include "THUNK.h"

template<typename TypeFunction>
class Hooker
{
public:
    Hooker(const wchar_t* szModuleName, const char* szFunctionName) :
        pOriginalFunction(reinterpret_cast<TypeFunction*>(GetProcAddress(GetModuleHandle(szModuleName), szFunctionName))),
        pHookThunk(new THUNK()),
        isHooked(false)
    {
    }
    ~Hooker()
    {
        delete this->pHookThunk;
    }

    void SetHookFunction(TypeFunction* pHookFunction)
    {
        this->pHookThunk->Set(pHookFunction);
    }
    template<typename T, typename C, typename I>
    void SetHookFunction(T C::* pHookMemberFunction, I* pInstance)
    {
        this->pHookThunk->Set(pHookMemberFunction, pInstance);
    }

    void Hook()
    {
        DetourTransactionBegin();
        DetourUpdateThread(GetCurrentThread());
        DetourAttach(&(PVOID&) this->pOriginalFunction, this->pHookThunk);
        this->isHooked = DetourTransactionCommit() == NO_ERROR;
    }
    void Unhook()
    {
        DetourTransactionBegin();
        DetourUpdateThread(GetCurrentThread());
        DetourDetach(&(PVOID&) this->pOriginalFunction, this->pHookThunk);
        this->isHooked = DetourTransactionCommit() != NO_ERROR;
    }

    TypeFunction* GetOriginalFunction()
    {
        return this->pOriginalFunction;
    }

private:
    TypeFunction* pOriginalFunction;
    THUNK* pHookThunk;
    bool isHooked;
};
