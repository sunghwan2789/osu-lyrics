#pragma once

template<typename F>
class Hooker
{
private:
    F pHookFunction;
    bool bHooked;

public:
    F pOriginalFunction;

    Hooker(const char *, const char *, F = nullptr);
    ~Hooker();

    void Set(F);

    void Hook();
    void Unhook();
};
