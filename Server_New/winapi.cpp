#include "Core.h"

Core ServerCore;

BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved)
{
    if (fdwReason == DLL_PROCESS_ATTACH)
    {
        ServerCore.Init();
    }
    else if (fdwReason == DLL_PROCESS_DETACH)
    {
        ServerCore.Release();
    }
    return TRUE;
}