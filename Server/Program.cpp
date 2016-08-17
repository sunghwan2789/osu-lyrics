#include <Windows.h>
#include "Server.h"
#include "HookP.h"

Server *server;
HookP *observer;

BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved)
{
    if (fdwReason == DLL_PROCESS_ATTACH)
    {
        server = new Server();
        server->Start();
        observer = new HookP();
        observer->Attach(server);
        observer->Run();
    }
    else if (fdwReason == DLL_PROCESS_DETACH)
    {
        observer->Stop();
        observer->Detach(server);
        server->Stop();
        delete observer;
        delete server;
    }
    return TRUE;
}
