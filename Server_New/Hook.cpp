#include "Core.h"

const BYTE szBinaryBlock = 5;

void HookBase::Init(Heap *pSharedHeap)
{
    pMutex = new std::mutex;
    HeapObject hbTmpObject = nullptr;

    pSharedHeap->AllocHeap(szBinaryBlock, hbTmpObject);

    this->pFuncRef = hbTmpObject;
}

void HookBase::Release()
{
    this->UnHook();
    
    delete pMutex;
    this->pBaseHeap->ReleaseHeap(pFuncRef);
}