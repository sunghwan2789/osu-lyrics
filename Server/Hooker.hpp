#include <cwchar>

#include <Windows.h>
#include "detours.h"

template<typename T>
Hooker<T>::Hooker(const wchar_t *szModuleName, const char *szFunctionName, T *pHookFunction = nullptr)
{
    this->hooked = false;

    this->pFunction = (T *) GetProcAddress(GetModuleHandle(szModuleName), szFunctionName);
    this->SetHookFunction(pHookFunction);
}

template<typename T>
Hooker<T>::~Hooker()
{
    this->Unhook();
}

template<typename T>
T *Hooker<T>::GetFunction()
{
    return this->pFunction;
}

template<typename T>
void Hooker<T>::SetHookFunction(T *pHookFunction)
{
    this->pHookFunction = pHookFunction;
}

template<typename T>
void Hooker<T>::Hook()
{
    if (this->hooked || this->pHookFunction == nullptr)
    {
        return;
    }

    DetourTransactionBegin();
    DetourUpdateThread(GetCurrentThread());
    DetourAttach(&(PVOID&) this->pFunction, (PVOID) this->pHookFunction);
    this->hooked = DetourTransactionCommit() == NO_ERROR;
}

template<typename T>
void Hooker<T>::Unhook()
{
    if (!this->hooked)
    {
        return;
    }

    DetourTransactionBegin();
    DetourUpdateThread(GetCurrentThread());
    DetourDetach(&(PVOID&) this->pFunction, (PVOID) this->pHookFunction);
    this->hooked = DetourTransactionCommit() != NO_ERROR;
}
