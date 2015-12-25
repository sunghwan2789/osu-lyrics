#include <Windows.h>
#include <cstring>
#include "mhook-lib/mhook.h"
#include "Hooker.h"

template<typename F>
Hooker<F>::Hooker(const char *moduleName, const char *functionName, F hkFunction = nullptr)
{
    this->bHooked = false;

    this->pOriginalFunction = (F) GetProcAddress(GetModuleHandle(moduleName), functionName);
    this->Set(hkFunction);
}

template<typename F>
Hooker<F>::~Hooker()
{
    this->Unhook();
}

template<typename F>
void Hooker<F>::Set(F hkFunction)
{
    this->pHookFunction = hkFunction;
}

template<typename F>
void Hooker<F>::Hook()
{
    if (this->bHooked && this->pHookFunction)
    {
        return;
    }

    this->bHooked = !!Mhook_SetHook((PVOID *) &this->pOriginalFunction, this->pHookFunction);
}

template<typename F>
void Hooker<F>::Unhook()
{
    if (!this->bHooked)
    {
        return;
    }

    this->bHooked = !Mhook_Unhook((PVOID *) &this->pHookFunction);
}

// explicit instantiation
#include "Observer.h"
template class Hooker<tReadFile>;
template class Hooker<tBASS_ChannelPlay>;
template class Hooker<tBASS_ChannelSetPosition>;
template class Hooker<tBASS_ChannelSetAttribute>;
template class Hooker<tBASS_ChannelPause>;
