#pragma once

const BYTE szOpcode = 5;
const BYTE asmJmp = 0xE9;

BOOL WINAPI hkReadFile(HANDLE hFile, LPVOID lpBuffer, DWORD nNumberOfBytesToRead, LPDWORD lpNumberOfBytesRead, LPOVERLAPPED lpOverlapped);
VOID WINAPI hkPostQuitMessage(int iMessage);

typedef class __ExcuteableObject : public __HeapObject
{
public:
    template<typename _pointer_func_type>
    _pointer_func_type Run();
}*ExcuteableObject;

template<typename _pointer_function>
class HookBase : public Base
{
protected:
    DWORD dwFuncRefAddr;
    BYTE OrignalOpcode[szOpcode];

    Heap *pBaseHeap;

    _pointer_function lFunction;
    ExcuteableObject pFuncRef;

public:
    void SetFunction(_pointer_function Func);

    void Hook(char moduleName[], char funcName[]);
    void UnHook();

    void Init(Heap *pSharedHeap);
    void Release();
};

template <typename _func_type>
void CreateHookObject(HookBase<_func_type> *&Object, _func_type toCall, Heap *pSharedHeap);