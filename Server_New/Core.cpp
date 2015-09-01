#include "Core.h"

void Core::Init()
{

}

void Core::Release()
{
    ReleaseBaseObject(pHkReadFile);
    ReleaseBaseObject(pHkPostQuitMessage);
}