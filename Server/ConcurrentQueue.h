#pragma once

#include <Windows.h>
#include <queue>

template<typename T> class ConcurrentQueue
{
private:
    std::queue<T *> Queue;
    CRITICAL_SECTION hMutex;
    HANDLE hPushed;

public:
    ConcurrentQueue();
    ~ConcurrentQueue();

    void Push(const T &);
    T Pop();
    void Clear();

    bool Empty();
    bool WaitPush(DWORD);
};
