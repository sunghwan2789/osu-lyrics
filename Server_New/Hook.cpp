#include "Core.h"

template<typename _pointer_function>
void HookBase<_pointer_function>::Init(Heap *pSharedHeap)
{
    HeapObject hbTmpObject = nullptr;

    pSharedHeap->AllocHeap(sizeof(DWORD), PAGE_EXECUTE_READWRITE, hbTmpObject);

    this->pFuncRef = (ExcuteableObject)hbTmpObject;
}

template<typename _pointer_function>
void HookBase<_pointer_function>::Release()
{
    this->UnHook();
    
    this->pBaseHeap->ReleaseHeap<ExcuteableObject>(pFuncRef);
}

template<typename _pointer_function>
void HookBase<_pointer_function>::Hook(char moduleName[], char funcName[])
{
    DWORD dwOldProtect = NULL;

    DWORD dwNewAddress = NULL;
    DWORD dwOldAddress = NULL;

    this->dwFuncRefAddr = (DWORD)GetProcAddress(GetModuleHandleA(moduleName), funcName);
    OrignalOpcode[0] = asmJmp;
    memcpy(&OrignalOpcode, (LPVOID)dwFuncRefAddr, szOpcode);

    VirtualProtect(LPVOID(this->dwFuncRefAddr), szOpcode, PAGE_EXECUTE_READWRITE, &dwOldProtect);

    dwOldAddress = (DWORD)(&this->dwFuncRefAddr) - DWORD(this->pFuncRef->Object()) - szOpcode;
    memcpy(this->pFuncRef->Object(), (LPVOID)dwOldAddress, sizeof(DWORD));

    dwNewAddress = DWORD(this->GetFunction()) - DWORD(this->dwFuncRefAddr) - szOpcode;

    memcpy(LPVOID(this->dwFuncRefAddr), &asmJmp, sizeof(BYTE));
    memcpy(LPVOID(this->dwFuncRefAddr + 1), (LPVOID)dwNewAddress, sizeof(DWORD));

    VirtualProtect(LPVOID(this->dwFuncRefAddr), szOpcode, dwOldProtect, nullptr);

    return;
}

template<typename _pointer_function>
void HookBase<_pointer_function>::UnHook()
{
    DWORD dwOldProtect = NULL;

    VirtualProtect(LPVOID(this->dwFuncRefAddr), szOpcode, PAGE_EXECUTE_READWRITE, &dwOldProtect);

    memcpy(LPVOID(this->dwFuncRefAddr), &OrignalOpcode, szOpcode);

    VirtualProtect(LPVOID(this->dwFuncRefAddr), szOpcode, dwOldProtect, nullptr);

    return;
}

template<typename _pointer_function>
void HookBase<_pointer_function>::SetFunction(_pointer_function Func)
{
    this->lFunction = Func;
}

template <typename _func_type>
void CreateHookObject(HookBase<_func_type> *&Object, _func_type toCall, Heap *pSharedHeap)
{
    HookBase<_func_type> *tmpObject = new HookBase<_func_type>;

    tmpObject->Init(pSharedHeap);
    tmpObject->SetFunction(toCall);

    Object = tmpObject;

    return;
}