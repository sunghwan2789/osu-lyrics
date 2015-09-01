#include "Core.h"

void Heap::Init()
{
    SYSTEM_INFO sys_info;
    LPVOID pTmpHeap;

    GetSystemInfo(&sys_info);

    pTmpHeap = VirtualAlloc(nullptr, sys_info.dwPageSize, MEM_RESERVE, PAGE_NOACCESS);
    if (pTmpHeap != nullptr)
    {
        this->szHeapMax = sys_info.dwPageSize;
        this->pHeap = pTmpHeap;
    }

    return;
}

void Heap::Release()
{

    std::list<HeapObject>::iterator it;
    for (it = this->pAllocated.begin(); it != pAllocated.end(); ++it)
    {
        delete (*it);
    }

    VirtualFree(this->pHeap, 0, MEM_RELEASE);
}