#include "Core.h"


void Heap::AllocPage()
{
    SYSTEM_INFO sys_info;
    DWORD pNextPage = NULL;

    GetSystemInfo(&sys_info);

    pNextPage = (DWORD)this->pHeap + this->szHeapMax;
    VirtualAlloc(LPVOID(pNextPage), sys_info.dwPageSize, MEM_RESERVE, PAGE_NOACCESS);
}


LPVOID Heap::AllocHeapEx(const size_t szHeap, const DWORD dwProtect)
{
    std::list<HeapObject>::iterator it;

    if (this->pAllocatedHeap.empty() && this->pCollectedHeap.empty())
    {
        return VirtualAlloc(this->pHeap, szHeap, MEM_COMMIT, dwProtect);
    }

    ZeroMemory(&it, sizeof(it));
    for (it = this->pCollectedHeap.begin(); it != pCollectedHeap.end(); ++it)
    {
        if ((*it)->GetSize()>szHeap) break;
    }

    if (it == pCollectedHeap.end())
    {
        ZeroMemory(&it, sizeof(it));
        for (it = this->pAllocatedHeap.begin(); it != pAllocatedHeap.end();)
        {
            DWORD dwFrontObject = NULL;
            DWORD dwNextObject = NULL;

            dwFrontObject = (DWORD)(*it)->Object() + (DWORD)(*it)->GetSize();
            dwNextObject = (DWORD)(*it)->Object();

            if (dwNextObject - dwFrontObject >= szHeap)
            {
                return VirtualAlloc(LPVOID(dwNextObject), szHeap, MEM_COMMIT, dwProtect);
            }
        }

    }
    else
    {
        DWORD dwAddrToAlloc = NULL;

        dwAddrToAlloc = (DWORD)(*it)->Object() + (DWORD)(*it)->GetSize();

        if (szHeap + dwAddrToAlloc < this->szHeapMax)
        {
            this->AllocPage();
        }

        return VirtualAlloc(LPVOID(dwAddrToAlloc), szHeap, MEM_COMMIT, dwProtect);

    }
}

bool Heap::AllocHeap(const size_t szHeap, const DWORD dwProtect, HeapObject &hbObject)
{
    LPVOID pObject = nullptr;
    HeapObject tmpObject = nullptr;

    pObject = AllocHeapEx(szHeap, dwProtect);

    if (pObject != nullptr)
    {
        tmpObject = new __HeapObject(pObject, dwProtect, this);
        hbObject = tmpObject;

        return true;
    }

    return false;
}

void Heap::ReleaseHeap(HeapObject &hbObject)
{
    std::list<HeapObject>::iterator it;

    for (it = this->pAllocatedHeap.begin(); it != pAllocatedHeap.end(); ++it)
    {
        if ((*it) == hbObject) {
            this->pAllocatedHeap.erase(it);
            break;
        }
    }

    VirtualFree(hbObject->Object(), hbObject->GetSize(), MEM_DECOMMIT);
    hbObject = nullptr;

    return;
}

void Heap::CollectHeap(HeapObject &hbObject)
{
    std::list<HeapObject>::iterator it;

    for (it = this->pAllocatedHeap.begin(); it != pAllocatedHeap.end(); ++it)
    {
        if ((*it) == hbObject) {
            this->pAllocatedHeap.erase(it);
            break;
        }
    }

    pCollectedHeap.push_back(hbObject);

    hbObject = nullptr;
    return;
}

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

    ZeroMemory(&it, sizeof(it));
    for (it = this->pAllocatedHeap.begin(); it != pAllocatedHeap.end(); ++it)
    {
        delete (*it);
    }

    ZeroMemory(&it, sizeof(it));
    for (it = this->pCollectedHeap.begin(); it != pCollectedHeap.end(); ++it)
    {
        delete (*it);
    }

    VirtualFree(this->pHeap, 0, MEM_RELEASE);
}