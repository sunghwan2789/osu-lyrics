#pragma once

#include <Windows.h>
#include <queue>
#include <mutex>

template <typename T>
class concurrent_queue
{
private:
    std::queue<T *> queue;
    std::mutex mutex;
    HANDLE hPushed;

public:
    concurrent_queue()
    {
        hPushed = CreateEventA(NULL, TRUE, FALSE, NULL);
    }

    ~concurrent_queue()
    {
        clear();
        CloseHandle(hPushed);
    }

    void push(const T data)
    {
        // 데이터를 그대로 큐에 넣으면
        // 오스의 힙을 데이터가 차지해서 메모리 커럽션남
        T *element = new T;

        *element = data;

        mutex.lock();
        queue.push(element);
        mutex.unlock();

        SetEvent(hPushed);
    }

    T pop()
    {
        T data, *element;

        mutex.lock();
        element = queue.front();
        queue.pop();
        mutex.unlock();

        data = *element;
        
        delete element;

        return data;
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
        std::queue<T *> newQueue;

        mutex.lock();
        newQueue.swap(queue);
        mutex.unlock();

        while (!queue.empty())
        {
            T *element = queue.front();
            queue.pop();

            delete element;
        }
    }
};