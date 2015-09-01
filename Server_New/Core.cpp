#include "Core.h"

void Core::Init()
{
    CreateHookObject<HkReadFile>(pHkReadFile);
    CreateHookObject<HkPostQuitMessage>(pHkPostQuitMessage);
}

void Core::Release()
{
    ReleaseBaseObject(pHkReadFile);
    ReleaseBaseObject(pHkPostQuitMessage);
}