#include <Windows.h>
#include "Server.h"
#include "Monitor.h"

Server* server;
Monitor* monitor;

BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved)
{
    if (fdwReason == DLL_PROCESS_ATTACH)
    {
        server = new Server();
        server->Start();
        monitor = new Monitor();
        monitor->Attach(static_cast<Observer*>(server));
        monitor->Activate();
    }
    else if (fdwReason == DLL_PROCESS_DETACH)
    {
        monitor->Disable();
        monitor->Detach(static_cast<Observer*>(server));
        server->Stop();
        delete monitor;
        server->Wait();
        delete server;
    }
    return TRUE;
}
