#include "THUNK.h"

HANDLE hHeap;
size_t nEntry;

void THUNK::Set(DWORD pFunction, DWORD pInstance)
{
    if (pInstance == 0)
    {
        //nop
        this->lea_ecx = 0x9090;
        this->pInstance = 0x90909090;
    }
    else
    {
        this->lea_ecx = 0x0D8D;
        this->pInstance = pInstance;
    }
    this->mov_eax = 0xB8;
    this->pFunction = pFunction;
    this->jmp_eax = 0xE0FF;
    // flush multi thread instruction cache
    FlushInstructionCache(GetCurrentProcess(), static_cast<void*>(this), sizeof(THUNK));
}

void* THUNK::operator new(size_t)
{
    if (nEntry == 0)
    {
        hHeap = HeapCreate(HEAP_CREATE_ENABLE_EXECUTE, 0, 0);
    }
    void* entry = HeapAlloc(hHeap, HEAP_GENERATE_EXCEPTIONS, sizeof(THUNK));
    nEntry++;
    return entry;
}

void THUNK::operator delete(void* p)
{
    HeapFree(hHeap, HEAP_GENERATE_EXCEPTIONS, p);
    if (--nEntry == 0)
    {
        HeapDestroy(hHeap);
    }
}
