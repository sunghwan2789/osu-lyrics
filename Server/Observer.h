#pragma once

#include <string>

struct Observer
{
    virtual void Update(const std::wstring&) = 0;
};
