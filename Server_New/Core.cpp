#include "Core.h"

void Core::Init()
{

    CreateHookObject<tReadFile*>(&ReadFileHook, &hkReadFile, pHeap);
    CreateHookObject<tPostQuitMessage*>(&PostQuitMessageHook, &hkPostQuitMessage, pHeap);
}

void Core::Release()
{
    ReleaseBaseObject(ReadFileHook);
    ReleaseBaseObject(PostQuitMessageHook);
}