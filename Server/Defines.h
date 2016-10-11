#pragma once

#include <Windows.h>
#include <assert.h>
#include <string>

#ifdef _DEBUG
#define DEBUG_ASSERT(expression) assert(expression)
#else
#define DEBUG_ASSERT()
#endif

#pragma warning(disable:4996)
