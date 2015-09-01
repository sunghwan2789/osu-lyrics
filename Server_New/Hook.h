#pragma once

typedef class HookBase : public Base
{
private:
    DWORD dwOrignalByte;
    DWORD dwNewNyte;

    Heap *pBaseHeap;
    HeapObject pFuncRef;

public:
    virtual void Hook() = 0;
    virtual void UnHook() = 0;

    void Init(Heap *pSharedHeap);
    void Release();
}*LPHOOK_BASE;

template <typename _hook_type>
void CreateHookObject(LPHOOK_BASE &Object);