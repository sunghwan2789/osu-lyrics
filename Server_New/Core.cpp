#include "Core.h"

void Core::Init()
{

    CreateHookObject(ReadFileHook, &hkReadFile, this->pHeap);
    CreateHookObject(PostQuitMessageHook, &hkPostQuitMessage, this->pHeap);
}

void Core::Release()
{
    ReleaseBaseObject(ReadFileHook);
    ReleaseBaseObject(PostQuitMessageHook);
}