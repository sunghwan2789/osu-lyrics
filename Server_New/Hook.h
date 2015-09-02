#pragma once

const BYTE szOpcode = 5;
const BYTE asmJmp = 0xE9;

typedef class __ExcuteableObject : public __HeapObject
{
public:
    template<typename _pointer_func_type>
    _pointer_func_type Run();
}*ExcuteableObject;

typedef class HookBase : public Base
{
protected:
    DWORD dwFuncRefAddr;

    Heap *pBaseHeap;
    ExcuteableObject pFuncRef;

public:
    virtual void Hook() = 0;
    virtual void UnHook() = 0;

    void Init(Heap *pSharedHeap);
    void Release();
}*LPHOOK_BASE;

template <typename _hook_type>
void CreateHookObject(LPHOOK_BASE &Object);