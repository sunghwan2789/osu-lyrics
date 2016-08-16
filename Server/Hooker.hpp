#include <tchar.h>

#include <Windows.h>
#include "detours.h"

template<typename TypeFunction>
Hooker<TypeFunction>::Hooker(const TCHAR *szModuleName, const char *szFunctionName, TypeFunction *pHookFunction = nullptr)
{
    this->isHooked = false;

    this->pFunction = reinterpret_cast<TypeFunction *>(GetProcAddress(GetModuleHandle(szModuleName), szFunctionName));
    this->SetHookFunction(pHookFunction);
}

template<typename TypeFunction>
Hooker<TypeFunction>::~Hooker()
{
    this->Unhook();
}

template<typename TypeFunction>
TypeFunction *Hooker<TypeFunction>::GetFunction()
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

	this->isHooked  = !!DetourAttach(&(PVOID&) this->pFunction, (PVOID) this->pHookFunction);
}

template<typename TypeFunction>
void Hooker<TypeFunction>::Unhook()
{
    if (!this->isHooked)
    {
        return;
    }

    
	this->isHooked  = !DetourDetach(&(PVOID&) this->pFunction, (PVOID) this->pHookFunction);
}
