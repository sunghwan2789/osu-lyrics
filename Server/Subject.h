#pragma once

#include "Observable.h"

#include <vector>

class Subject : public Observable
{
public:
    virtual void Attach(Observer*) override;
    virtual void Detach(Observer*) override;
    virtual void Notify(const std::wstring&) override;

private:
    std::vector<Observer*> observers;
};
