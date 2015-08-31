#pragma once

#include <Windows.h>

void ReleaseHandleObject(HANDLE &hObject)
{
    if (hObject != nullptr)
    {
        CloseHandle(hObject);
        hObject = nullptr;
    }
    return;
}