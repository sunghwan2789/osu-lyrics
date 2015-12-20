#include "Hooker.h"
#include <Windows.h>
#include <cstring>

template<typename F> Hooker<F>::Hooker(const char *moduleName, const char *funcName)
{
    InitializeCriticalSection(&this->hMutex);
    this->Hooked = false;

    this->pFunction = (F) GetProcAddress(GetModuleHandle(moduleName), funcName);
}

template<typename F> Hooker<F>::~Hooker()
{
    this->EnterCS();
    this->Unhook();
    this->LeaveCS();

    DeleteCriticalSection(&this->hMutex);
}

template<typename F> void Hooker<F>::Set(F hkFunction)
{
    this->hkFunction = hkFunction;
}

template<typename F> void Hooker<F>::Hook()
{
    if (this->Hooked)
    {
        return;
    }

    DWORD dwOldProtect, dwNewAddress;
    BYTE newOpcode[szOpcode] = { asmJmp };

    VirtualProtect(this->pFunction, szOpcode, PAGE_EXECUTE_READWRITE, &dwOldProtect);

    memcpy(this->OriginalOpcode, this->pFunction, szOpcode);

    dwNewAddress = (DWORD) this->hkFunction - (DWORD) this->pFunction - szOpcode;
    memcpy(newOpcode + 1, &dwNewAddress, szOpcode - 1);

    this->Hooked = memcpy(this->pFunction, newOpcode, szOpcode) != nullptr;

    VirtualProtect(this->pFunction, szOpcode, dwOldProtect, nullptr);
}

template<typename F> void Hooker<F>::Unhook()
{
    if (!this->Hooked)
    {
        return;
    }

    DWORD dwOldProtect;

    VirtualProtect(this->pFunction, szOpcode, PAGE_EXECUTE_READWRITE, &dwOldProtect);

    this->Hooked = memcpy(this->pFunction, this->OriginalOpcode, szOpcode) == nullptr;

    VirtualProtect(this->pFunction, szOpcode, dwOldProtect, nullptr);
}

template<typename F> void Hooker<F>::EnterCS()
{
    EnterCriticalSection(&this->hMutex);
}

template<typename F> void Hooker<F>::LeaveCS()
{
    LeaveCriticalSection(&this->hMutex);
}

// explicit instantiation
template class Hooker<tReadFile>;
