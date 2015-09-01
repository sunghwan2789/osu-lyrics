#pragma once

__interface Base
{
public:
    void Release();
};

void ReleaseBaseObject(Base *pObject); 
/* Utils.cpp에 서술됨. 오브젝트가 있는지 없는지 확인한 후에.
   안전하게 제거됨. 사용법=>ReleaseBaseObject(Object);  */