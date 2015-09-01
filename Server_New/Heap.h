#pragma once

typedef class __HeapObject : Base
{
private:
    size_t szHeap;
    LPVOID pOffset;
    DWORD dwProtect;

    Heap *pBaseHeap;
public:

    void SetProtection(DWORD &dwProtect);
    void GetProtection(DWORD &dwProtect);
    LPVOID Object();
}*HeapObject;

class Heap : Base
{
private:
    LPVOID pHeap;

    size_t szHeapUsing;
    size_t szHeapMax;

    std::list<HeapObject> pAllocated;
    std::list<LPVOID> pCollectedHeap;
    std::list<size_t> szCollectedHeap;

public:
    void Release();
    void Init();

    bool AllocHeap(size_t szHeap, HeapObject &hbObject);
    bool ReleaseHeap(HeapObject &hbObject);
    bool CollectHeap(HeapObject &hbObject);
};