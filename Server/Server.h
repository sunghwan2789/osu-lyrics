#pragma once

#include <Windows.h>
#include <string>
#include "ConcurrentQueue.h"

#define BUF_SIZE MAX_PATH * 3

void PushMessage(const std::string &);
