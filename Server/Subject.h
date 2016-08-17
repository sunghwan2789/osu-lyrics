#pragma once

#include <string>
#include <vector>

#include "Observer.h"

class Subject
{
public:
    void Attach(Observer *);
    void Detach(Observer *);
    void Notify(std::wstring&&);

private:
    std::vector<Observer *> observers;
};
