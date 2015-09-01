#include "Core.h"


void ReleaseBaseObject(Base *pObject)
{
    if (&pObject != nullptr)
    {
        pObject->Release();
        ZeroMemory(pObject, sizeof(pObject));
    }
    return;
}