/** Thread-safe Singleton
 *
 * http://silviuardelean.ro/2012/06/05/few-singleton-approaches/
 */

// Singleton.h
#pragma once

#include <memory>
#include <mutex>

class Singleton
{
public:
    static Singleton *GetInstance()
    {
        std::call_once(Singleton::once_flag, []
        {
            Singleton::instance.reset(new Singleton, [](Singleton *p)
            {
                delete p;
            });
        });
        return Singleton::instance.get();
    }

private:
    Singleton() {}
    ~Singleton() {}

    Singleton(const Singleton&) = delete;
    Singleton(Singleton&&) = delete;
    Singleton& operator=(const Singleton&) = delete;
    Singleton& operator=(Singleton&&) = delete;

    static std::shared_ptr<Singleton> instance;
    static std::once_flag once_flag;
};

// Singleton.cpp
std::shared_ptr<Singleton> Singleton::instance;
std::once_flag once_flag;
