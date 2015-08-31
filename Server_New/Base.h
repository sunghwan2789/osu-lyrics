#pragma once

__interface Base
{
    void Init();
    void Release();
};

void ReleaseBaseObject(Base *&pObject)
{
    if (&pObject != nullptr)
    {
        pObject->Release();
        pObject = nullptr;
    }
    return;
}