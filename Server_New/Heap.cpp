#include "Core.h"

template<typename _pointer_func_type>
_pointer_func_type __ExcuteableObject::Run()
{
    return (_pointer_func_type)this->Object();
}

#define _HEAP
#ifdef _HEAP
void Heap::AllocPage()
{
    SYSTEM_INFO sys_info;
    DWORD pNextPage = NULL;

    GetSystemInfo(&sys_info);

    pNextPage = (DWORD)pHeap + szHeapMax;
    VirtualAlloc(LPVOID(pNextPage), sys_info.dwPageSize, MEM_RESERVE, PAGE_NOACCESS);
}


LPVOID Heap::AllocHeapEx(const size_t szHeap, const DWORD dwProtect)
{
    std::list<HeapObject>::iterator it;

    if (pAllocatedHeap.empty() && pCollectedHeap.empty())
    {
        return VirtualAlloc(this->pHeap, szHeap, MEM_COMMIT, dwProtect);
    }

    ZeroMemory(&it, sizeof(it));
    for (it = pCollectedHeap.begin(); it != pCollectedHeap.end(); ++it)
    {
        if ((*it)->GetSize()>szHeap) break;
    }

    if (it == pCollectedHeap.end())
    {
        ZeroMemory(&it, sizeof(it));
        for (it = pAllocatedHeap.begin(); it != pAllocatedHeap.end();)
        {
            DWORD dwFrontObject = NULL;
            DWORD dwNextObject = NULL;

            dwFrontObject = (DWORD)(*it)->Object() + (DWORD)(*it)->GetSize();
            dwNextObject = (DWORD)(*it++)->Object();

            if (dwNextObject - dwFrontObject >= szHeap)
            {
                return VirtualAlloc(LPVOID(dwFrontObject), szHeap, MEM_COMMIT, dwProtect);
            }
        }

    }
    else
    {
        DWORD dwAddrToAlloc = NULL;

        dwAddrToAlloc = (DWORD)(*it)->Object() + (DWORD)(*it)->GetSize();

        if (szHeap + dwAddrToAlloc < szHeapMax)
        {
            this->AllocPage();
        }

        return VirtualAlloc(LPVOID(dwAddrToAlloc), szHeap, MEM_COMMIT, dwProtect);

    }

    return nullptr;
}

bool Heap::AllocHeap(const size_t szHeap, const DWORD dwProtect, HeapObject &hbObject)
{
    LPVOID pObject = nullptr;
    HeapObject tmpObject = nullptr;

    pObject = AllocHeapEx(szHeap, dwProtect);

    if (pObject != nullptr)
    {
        tmpObject = new __HeapObject(pObject, szHeap, dwProtect);
        hbObject = tmpObject;

        return true;
    }

    return false;
}

template<typename _type_object>
void Heap::ReleaseHeap(_type_object &hbObject)
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

template<typename _type_object>
void Heap::CollectHeap(_type_object &hbObject)
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
#endif

#define _HEAP_OBJECT
#ifdef _HEAP_OBJECT
__HeapObject::__HeapObject(LPVOID object, size_t size, DWORD protect)
{
    this->pObject = object;
    this->szHeap = size;
    this->dwProtect = protect;
}

size_t __HeapObject::GetSize() { return this->szHeap; }
DWORD __HeapObject::GetProtection() { return this->dwProtect; }
DWORD __HeapObject::SetProtection(DWORD dwProtect)
{
    DWORD dwOldProtect;
    VirtualProtect(pObject, szHeap, dwProtect, &dwOldProtect);
    this->dwProtect = dwProtect;

    return dwOldProtect;
}

LPVOID __HeapObject::Object() { return this->pObject; }
#endif