#pragma once

const BYTE szOpcode = 5;
const BYTE asmJmp = 0xE9;

BOOL WINAPI hkReadFile(HANDLE, LPVOID, DWORD, LPDWORD, LPOVERLAPPED);
VOID WINAPI hkPostQuitMessage(int);


typedef class __ExcuteableObject : public __HeapObject
{
public:
    template<typename _pointer_func_type>
    _pointer_func_type Run()
    {
        return (_pointer_func_type)this->Object();
    }
}*ExcuteableObject;


template<typename _pointer_function>
class HookBase : public Base
{
protected:
    DWORD dwFuncRefAddr;
    BYTE OrignalOpcode[szOpcode];

    Heap *pBaseHeap;

    _pointer_function lFunction;
    ExcuteableObject pFuncRef;

public:

    void SetFunction(_pointer_function pFunc)
    {
        this->lFunction = pFunc;
    }

    void HookBaseSetFunction(_pointer_function Func)
    {
        this->lFunction = Func;
    }

    void HookBaseHook(char moduleName[], char funcName[])
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

        dwNewAddress = DWORD(this->lFunction) - DWORD(this->dwFuncRefAddr) - szOpcode;

        memcpy(LPVOID(this->dwFuncRefAddr), &asmJmp, sizeof(BYTE));
        memcpy(LPVOID(this->dwFuncRefAddr + 1), (LPVOID)dwNewAddress, sizeof(DWORD));

        VirtualProtect(LPVOID(this->dwFuncRefAddr), szOpcode, dwOldProtect, nullptr);

        return;
    }

    void UnHook()
    {
        DWORD dwOldProtect = NULL;

        VirtualProtect(LPVOID(this->dwFuncRefAddr), szOpcode, PAGE_EXECUTE_READWRITE, &dwOldProtect);

        memcpy(LPVOID(this->dwFuncRefAddr), &OrignalOpcode, szOpcode);

        VirtualProtect(LPVOID(this->dwFuncRefAddr), szOpcode, dwOldProtect, nullptr);

        return;
    }

    void Init(Heap *pSharedHeap)
    {
        HeapObject hbTmpObject = nullptr;

        pSharedHeap->AllocHeap(sizeof(DWORD), PAGE_EXECUTE_READWRITE, hbTmpObject);

        this->pFuncRef = (ExcuteableObject)hbTmpObject;
    }

    void Release()
    {
        this->UnHook();

        this->pBaseHeap->ReleaseHeap((HeapObject*)&pFuncRef);
    }
};

template <typename _func_type>
void CreateHookObject(HookBase<_func_type> **Object, _func_type toCall, Heap *pSharedHeap)
{
    HookBase<_func_type> *tmpObject = new HookBase<_func_type>;

    tmpObject->Init(pSharedHeap);
    tmpObject->SetFunction(toCall);

    *Object = tmpObject;

    return;
}