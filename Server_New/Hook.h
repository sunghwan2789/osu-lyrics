#pragma once

class HookBase : public Base
{
private:
    DWORD dwOrignalByte;
    DWORD dwNewNyte;

    std::mutex *pMutex;

    Heap *pBaseHeap;
    HeapObject pFuncRef;

public:
    virtual void Hook() = 0;
    virtual void UnHook() = 0;

    void Init(Heap *pSharedHeap);
    void Release();
};