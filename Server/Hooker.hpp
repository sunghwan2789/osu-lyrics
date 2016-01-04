#include <tchar.h>

#include <Windows.h>
#include "mhook-lib/mhook.h"

template<typename T>
Hooker<T>::Hooker(const TCHAR *szModuleName, const char *szFunctionName, T *pHookFunction = nullptr)
{
    this->hooked = false;

    this->pFunction = reinterpret_cast<T *>(GetProcAddress(GetModuleHandle(szModuleName), szFunctionName));
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

    this->hooked = static_cast<bool>(Mhook_SetHook(reinterpret_cast<PVOID *>(&this->pFunction), reinterpret_cast<PVOID>(this->pHookFunction)));
}

template<typename T>
void Hooker<T>::Unhook()
{
    if (!this->hooked)
    {
        return;
    }

    this->hooked = !static_cast<bool>(Mhook_Unhook(reinterpret_cast<PVOID *>(&this->pHookFunction)));
}
