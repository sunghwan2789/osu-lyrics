#include <Windows.h>
#include <cstring>
#include "mhook-lib/mhook.h"
#include "Hooker.h"

template<typename functype>
Hooker<functype>::Hooker(const char *moduleName, const char *functionName, LPVOID hkFunction = nullptr)
{
    this->bHooked = false;

    this->pOriginFunc = GetProcAddress(GetModuleHandle(moduleName), functionName);
    this->Set(hkFunction);
}

template<typename functype>
functype *Hooker<functype>::Get()
{
	return (functype*)this->pOriginFunc;
}

template<typename functype>
void Hooker<functype>::Set(LPVOID hkFunction)
{
    this->pHookFunc = hkFunction;
}

template<typename functype>
void Hooker<functype>::Hook()
{
    if (bHooked || !pHookFunc)
    {
        return;
    }

    this->bHooked = !!Mhook_SetHook(&(this->pOriginFunc), this->pHookFunc);
}

template<typename functype>
void Hooker<functype>::Unhook()
{
    if (!this->bHooked)
    {
        return;
    }

    this->bHooked = !Mhook_Unhook(&(this->pHookFunc));
}

// explicit instantiation
#include "Observer.h"
template class Hooker<tReadFile>;
template class Hooker<tBASS_ChannelPlay>;
template class Hooker<tBASS_ChannelSetPosition>;
template class Hooker<tBASS_ChannelSetAttribute>;
template class Hooker<tBASS_ChannelPause>;
