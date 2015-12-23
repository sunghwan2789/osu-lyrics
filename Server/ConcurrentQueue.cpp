#include <Windows.h>
#include <queue>
#include "ConcurrentQueue.h"

template<typename T> ConcurrentQueue<T>::ConcurrentQueue()
{
    InitializeCriticalSection(&this->hMutex);
    this->hPushed = CreateEvent(NULL, TRUE, FALSE, NULL);
}

template<typename T> ConcurrentQueue<T>::~ConcurrentQueue()
{
    this->Clear();
    CloseHandle(this->hPushed);
    DeleteCriticalSection(&this->hMutex);
}

template<typename T> void ConcurrentQueue<T>::Push(const T &data)
{
    // 데이터를 그대로 큐에 넣으면
    // osu!의 힙을 데이터가 차지해서 메모리 커럽션남
    T *element = new T;

    *element = data;

    EnterCriticalSection(&this->hMutex);
    this->Queue.push(element);
    LeaveCriticalSection(&this->hMutex);

    SetEvent(this->hPushed);
}

template<typename T> T ConcurrentQueue<T>::Pop()
{
    T data, *element;

    EnterCriticalSection(&this->hMutex);
    element = this->Queue.front();
    this->Queue.pop();
    LeaveCriticalSection(&this->hMutex);

    data = *element;

    delete element;

    return data;
}

template<typename T> void ConcurrentQueue<T>::Clear()
{
    std::queue<T *> cleanQueue;

    EnterCriticalSection(&this->hMutex);
    cleanQueue.swap(this->Queue);
    LeaveCriticalSection(&this->hMutex);

    // 주의!! this->Queue is swapped with cleanQueue
    while (!cleanQueue.empty())
    {
        T *element = cleanQueue.front();
        cleanQueue.pop();

        delete element;
    }
}

template<typename T> bool ConcurrentQueue<T>::Empty()
{
    bool result;

    EnterCriticalSection(&this->hMutex);
    result = this->Queue.empty();
    LeaveCriticalSection(&this->hMutex);

    return result;
}

template<typename T> bool ConcurrentQueue<T>::WaitPush(DWORD ms)
{
    if (WaitForSingleObject(this->hPushed, ms) == WAIT_OBJECT_0)
    {
        ResetEvent(this->hPushed);
        return true;
    }
    return false;
}

// explicit instantiation
#include <string>
template class ConcurrentQueue<std::string>;
