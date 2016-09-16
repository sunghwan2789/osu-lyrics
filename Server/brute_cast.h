#pragma once

template<typename R, typename T>
R brute_cast(T in)
{
    union
    {
        R o;
        T i;
    } u;
    u.i = in;
    return u.o;
}
