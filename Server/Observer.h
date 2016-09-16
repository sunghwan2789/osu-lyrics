#pragma once

#include <string>

class Observer
{
public:
    virtual void Update(const std::wstring&) = 0;
};
