#pragma once

#include <Windows.h>
#include <queue>
#include <mutex>

template <typename T>
class concurrent_queue
{
private:
    std::queue<T*> queue;
    std::mutex mutex;
    HANDLE hPushed;

public:
    concurrent_queue()
    {
        hPushed = CreateEventA(NULL, TRUE, FALSE, NULL);
    }

    ~concurrent_queue()
    {
        CloseHandle(hPushed);
    }

    void push(const T elem)
    {
        T *tmpData;
        tmpData = new T;
        memcpy(tmpData, &elem, sizeof(elem));

        mutex.lock();
        queue.push(tmpData);
        mutex.unlock();
        SetEvent(hPushed);
    }

    T pop()
    {
        T front, *tmpData;

        mutex.lock();
        tmpData = queue.front();
        queue.pop();
        mutex.unlock();

        memcpy(&front, (void*)tmpData, sizeof(tmpData));

        delete tmpData;

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
        std::queue<T*> toChange;

        mutex.lock();
        toChange.swap(queue);
        mutex.unlock();

        while (!queue.empty())
        {
            T *tmpData;
            tmpData = queue.front();
            queue.pop();

            delete tmpData;
        }
    }
};