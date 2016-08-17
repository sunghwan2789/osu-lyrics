#include <cwchar>

#include <Windows.h>
#include "detours.h"

template<typename TypeFunction>
Hooker<TypeFunction>::Hooker(const wchar_t *szModuleName, const char *szFunctionName, TypeFunction *pHookFunction = nullptr)
{
    this->isHooked = false;

    this->pFunction = (TypeFunction *) GetProcAddress(GetModuleHandle(szModuleName), szFunctionName);
    this->SetHookFunction(pHookFunction);
}

template<typename TypeFunction>
Hooker<TypeFunction>::~Hooker()
{
    this->Unhook();
}

template<typename TypeFunction>
TypeFunction *Hooker<TypeFunction>::GetOriginalFunction()
{
    return this->pFunction;
}

template<typename TypeFunction>
void Hooker<TypeFunction>::SetHookFunction(TypeFunction *pHookFunction)
{
    this->pHookFunction = pHookFunction;
}

template<typename TypeFunction>
void Hooker<TypeFunction>::Hook()
{
    if (this->isHooked || this->pHookFunction == nullptr)
    {
        return;
    }

    DetourTransactionBegin();
    DetourUpdateThread(GetCurrentThread());
    DetourAttach(&(PVOID&) this->pFunction, (PVOID) this->pHookFunction);
    this->isHooked = DetourTransactionCommit() == NO_ERROR;
}

template<typename TypeFunction>
void Hooker<TypeFunction>::Unhook()
{
    if (!this->isHooked)
    {
        return;
    }

    DetourTransactionBegin();
    DetourUpdateThread(GetCurrentThread());
    DetourDetach(&(PVOID&) this->pFunction, (PVOID) this->pHookFunction);
    this->isHooked = DetourTransactionCommit() != NO_ERROR;
}
