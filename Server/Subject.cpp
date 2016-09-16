#include "Subject.h"

#include <algorithm>
#include <utility>

void Subject::Attach(Observer* observer)
{
    this->observers.emplace_back(observer);
}

void Subject::Detach(Observer* observer)
{
    this->observers.erase(std::find(this->observers.begin(), this->observers.end(), observer), this->observers.end());
}

void Subject::Notify(const std::wstring& message)
{
    std::for_each(this->observers.begin(), this->observers.end(), [&message](auto& observer)
    {
        observer->Update(message);
    });
}
