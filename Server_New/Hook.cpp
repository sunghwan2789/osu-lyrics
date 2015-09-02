#include "Core.h"

const BYTE szBinaryBlock = 5;

void HookBase::Init(Heap *pSharedHeap)
{
    HeapObject hbTmpObject = nullptr;

    pSharedHeap->AllocHeap(szBinaryBlock, PAGE_EXECUTE_READWRITE, hbTmpObject);

    this->pFuncRef = (ExcuteableObject)hbTmpObject;
}

void HookBase::Release()
{
    this->UnHook();
    
    this->pBaseHeap->ReleaseHeap<ExcuteableObject>(pFuncRef);
}

template <typename _hook_type>
void CreateHookObject(LPHOOK_BASE &Object)
{
    Object = new _hook_type;
    return;
}