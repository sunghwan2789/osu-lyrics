#include "Thread.h"

Thread::Thread()
    : pThunk(new THUNK())
{
    this->pThunk->Set(&Thread::Proc, this);
}

Thread::~Thread()
{
    delete this->pThunk;
}

void Thread::Start()
{
    this->hThread = CreateThread(NULL, 0, reinterpret_cast<LPTHREAD_START_ROUTINE>(this->pThunk), NULL, 0, NULL);
}

void Thread::Wait()
{
    WaitForSingleObject(this->hThread, INFINITE);
    CloseHandle(this->hThread);
}

DWORD Thread::Proc(LPVOID lParam)
{
    this->Run();
    return 0;
}
