#pragma once

#include <Windows.h>
#include "brute_cast.h"

struct THUNK
{
#pragma pack(push, 1)
    //lea ecx,pInstance
    WORD lea_ecx;
    DWORD pInstance;
    //mov eax,pFunction
    BYTE mov_eax;
    DWORD pFunction;
    //jmp eax
    WORD jmp_eax;
#pragma pack(pop)

    THUNK()
        : lea_ecx(0),
        pInstance(0),
        mov_eax(0),
        pFunction(0),
        jmp_eax(0)
    {
    }

    template<typename T>
    void Set(T* pFunction)
    {
        this->Set(reinterpret_cast<DWORD>(pFunction), reinterpret_cast<DWORD>(nullptr));
    }
    template<typename T, typename C, typename I>
    void Set(T C::* pMemberFunction, I* pInstance)
    {
        this->Set(brute_cast<DWORD>(pMemberFunction), reinterpret_cast<DWORD>(pInstance));
    }
    void Set(DWORD, DWORD);

    void* operator new(size_t);
    void operator delete(void*);
};
