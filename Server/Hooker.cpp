#include <Windows.h>
#include <cstring>
#include "Hooker.h"

template<typename F>
Hooker<F>::Hooker(const char *moduleName, const char *functionName, F hkFunction = nullptr)
{
    InitializeCriticalSection(&this->hMutex);
    this->Hooked = false;

    this->pFunction = (F) GetProcAddress(GetModuleHandle(moduleName), functionName);
    this->Set(hkFunction);
}

template<typename F>
Hooker<F>::~Hooker()
{
    this->EnterCS();
    this->Unhook();
    this->LeaveCS();

    DeleteCriticalSection(&this->hMutex);
}

template<typename F>
void Hooker<F>::Set(F hkFunction)
{
    this->hkFunction = hkFunction;
}

template<typename F>
void Hooker<F>::Hook()
{
    if (this->Hooked && this->hkFunction)
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

template<typename F>
void Hooker<F>::Unhook()
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

template<typename F>
inline void Hooker<F>::EnterCS()
{
    EnterCriticalSection(&this->hMutex);
}

template<typename F>
inline void Hooker<F>::LeaveCS()
{
    LeaveCriticalSection(&this->hMutex);
}

// explicit instantiation
#include "Observer.h"
template class Hooker<tReadFile>;
template class Hooker<tBASS_ChannelPlay>;
template class Hooker<tBASS_ChannelSetPosition>;
template class Hooker<tBASS_ChannelSetAttribute>;
template class Hooker<tBASS_ChannelPause>;