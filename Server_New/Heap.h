#pragma once

typedef class __HeapObject
{
private:
    LPVOID pObject;
    DWORD dwProtect;
    size_t szHeap;

    Heap *pBaseHeap;
public:
    __HeapObject(LPVOID object, size_t size, DWORD protect, Heap *pHeap);
    size_t GetSize();

    DWORD SetProtection(DWORD dwProtect);
    DWORD GetProtection();
    LPVOID Object();
}*HeapObject;

class Heap : Base
{
private:
    LPVOID pHeap;

    size_t szHeapUsing;
    size_t szHeapMax;

    std::list<HeapObject> pAllocatedHeap;
    std::list<HeapObject> pCollectedHeap;

    void AllocPage();
    LPVOID AllocHeapEx(const size_t szHeap, const DWORD dwProtect);

public:
    void Release();
    void Init();

    bool AllocHeap(size_t szHeap, DWORD dwProtect, HeapObject &hbObject);
    void ReleaseHeap(HeapObject &hbObject);
    void CollectHeap(HeapObject &hbObject);
};