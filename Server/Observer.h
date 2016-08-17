#pragma once

#include <string>

class Observer
{
public:
    virtual void Update(std::wstring&&) = 0;
};
