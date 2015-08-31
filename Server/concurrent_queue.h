#pragma once

#include <Windows.h>
#include <queue>
#include <mutex>

template <typename T>
class concurrent_queue
{
private:
    std::queue<T> queue;
    std::mutex mutex;
    HANDLE hPushed = NULL;

public:
    concurrent_queue()
    {
        hPushed = CreateEventA(NULL, TRUE, FALSE, NULL);
    }

    ~concurrent_queue()
    {
        CloseHandle(hPushed);
    }

    void push(const T &elem)
    {
        std::lock_guard<std::mutex> lock(mutex);
        queue.push(elem);
        SetEvent(hPushed);
    }

    T &pop()
    {
        std::lock_guard<std::mutex> lock(mutex);
        T &front = queue.front();
        queue.pop();
        return front;
    }

    bool empty()
    {
        return queue.empty();
    }

    bool wait_push(DWORD ms)
    {
        return WaitForSingleObject(hPushed, ms) == WAIT_OBJECT_0;
    }

    void clear()
    {
        std::lock_guard<std::mutex> lock(mutex);
        std::queue<T>().swap(queue);
    }
};