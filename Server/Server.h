#pragma once

#include <tchar.h>
#include <string>

#include <Windows.h>

typedef std::basic_string<TCHAR> tstring;

const DWORD nBufferSize = MAX_PATH * 3;

void PushMessage(tstring &&);
