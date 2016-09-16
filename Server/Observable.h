#pragma once

#include <string>

#include "Observer.h"

struct Observable
{
    virtual void Attach(Observer*) = 0;
    virtual void Detach(Observer*) = 0;
    virtual void Notify(const std::wstring&) = 0;
};
