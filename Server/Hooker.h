#pragma once

template<typename functype>
class Hooker
{
private:
    LPVOID pHookFunc;
	LPVOID pOriginFunc;
    bool bHooked;

public:

    Hooker(const char *, const char *, LPVOID = nullptr);

	functype *Get();
    void Set(LPVOID);

    void Hook();
    void Unhook();
};
