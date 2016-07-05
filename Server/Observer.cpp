#pragma comment(lib, "Shlwapi.lib")
#pragma comment(lib, "opengl32.lib")

#pragma warning(disable:4996)

#include "Observer.h"

#include <cstdio>
#include <cstdlib>
#include <tchar.h>
#include <string>
#include <utility>
#include <functional>

#include <gl/GL.h>
#include <gl/GLU.h>
#include <Windows.h>
#include <Shlwapi.h>
#include "bass.h"
#include "bass_fx.h"
#include "Hooker.h"
#include "Server.h"

#define AUDIO_FILE_INFO_TOKEN "AudioFilename:"

std::shared_ptr<Observer> Observer::instance;
std::once_flag Observer::once_flag;

BOOL WINAPI Observer::ReadFile(HANDLE hFile, LPVOID lpBuffer, DWORD nNumberOfBytesToRead, LPDWORD lpNumberOfBytesRead, LPOVERLAPPED lpOverlapped)
{
    Observer *instance = Observer::GetInstance();

    if (!instance->hookerReadFile.GetFunction()(hFile, lpBuffer, nNumberOfBytesToRead, lpNumberOfBytesRead, lpOverlapped))
    {
        return FALSE;
    }

    TCHAR szFilePath[MAX_PATH];
    DWORD nFilePathLength = GetFinalPathNameByHandle(hFile, szFilePath, MAX_PATH, VOLUME_NAME_DOS);
    //                  1: \\?\D:\Games\osu!\...
    DWORD dwFilePosition = SetFilePointer(hFile, 0, NULL, FILE_CURRENT) - (*lpNumberOfBytesRead);
    // 지금 읽는 파일이 비트맵 파일이고 앞부분을 읽었다면 음악 파일 경로 얻기:
    // AudioFilename은 앞부분에 있음 / 파일 핸들 또 열지 말고 일 한 번만 하자!
    if (wcsncmp(L".osu", &szFilePath[nFilePathLength - 4], 4) == 0 && dwFilePosition == 0)
    {
        // strtok은 소스를 변형하므로 일단 백업
        // .osu 파일은 UTF-8(Multibyte) 인코딩
		
		/* 줄마다 strtok으로 잘라내서 AudioFilename: 을 찾음. */
        char *buffer = strdup((const char*)(lpBuffer));

		for (char *line = strtok(buffer, "\n"); line != NULL; line = strtok(NULL, "\n"))
        {
            if (strnicmp(line, AUDIO_FILE_INFO_TOKEN, 14) != 0)
            {
                continue;
            }

            // AudioFilename 값 얻기
            TCHAR szAudioFileName[MAX_PATH];

            mbstowcs(szAudioFileName, &line[14], MAX_PATH);
            StrTrimW(szAudioFileName, L" \r");

            TCHAR szAudioFilePath[MAX_PATH];

			/* 앞부분의 이상한 글자를 제거하기위해 4번째 글자부터 시작. */
            wcscpy(szAudioFilePath, &szFilePath[4]);
            PathRemoveFileSpecW(szAudioFilePath);
            PathCombineW(szAudioFilePath, szAudioFilePath, szAudioFileName);

			EnterCriticalSection(&instance->hCritiaclSection);

			instance->currentPlaying.audioPath = tstring(szAudioFilePath);
			/* 앞부분의 이상한 글자를 제거하기위해 4번째 글자부터 시작. */
			instance->currentPlaying.beatmapPath = (tstring(&szFilePath[4]));

			LeaveCriticalSection(&instance->hCritiaclSection);

            break;
        }

        free(buffer);
    }
    return TRUE;
}


inline long long GetCurrentSysTime()
{
    long long t;
    GetSystemTimeAsFileTime(reinterpret_cast<LPFILETIME>(&t));
    return t;
}

BOOL WINAPI Observer::BASS_ChannelPlay(DWORD handle, BOOL restart)
{
    Observer *instance = Observer::GetInstance();

    if (!instance->hookerBASS_ChannelPlay.GetFunction()(handle, restart))
    {
        return FALSE;
    }

    BASS_CHANNELINFO info;
    BASS_ChannelGetInfo(handle, &info);

    if (info.ctype & BASS_CTYPE_STREAM)
    {
        double currentTimePos = BASS_ChannelBytes2Seconds(handle, BASS_ChannelGetPosition(handle, BASS_POS_BYTE));
        float tempo; BASS_ChannelGetAttribute(handle, BASS_ATTRIB_TEMPO, &tempo);
        instance->SendTempoInfomation(GetCurrentSysTime(), currentTimePos, tempo);
    }
    return TRUE;
}

BOOL WINAPI Observer::BASS_ChannelSetPosition(DWORD handle, QWORD pos, DWORD mode)
{
    Observer *instance = Observer::GetInstance();
    if (!instance->hookerBASS_ChannelSetPosition.GetFunction()(handle, pos, mode))
    {
        return FALSE;
    }

    BASS_CHANNELINFO info;
    BASS_ChannelGetInfo(handle, &info);

    if (info.ctype & BASS_CTYPE_STREAM)
    {
        double currentTime = BASS_ChannelBytes2Seconds(handle, pos);
        float CurrentTempo; BASS_ChannelGetAttribute(handle, BASS_ATTRIB_TEMPO, &CurrentTempo);
        // 주의!! pos가 일정 이하일 때,
        // 재생하면 BASS_ChannelPlay대신 이 함수가 호출되고,
        // BASS_ChannelIsActive 값은 BASS_ACTIVE_PAUSED임.
        if (BASS_ChannelIsActive(handle) == BASS_ACTIVE_PAUSED)
        {
            CurrentTempo = -100;
        }

        instance->SendTempoInfomation(GetCurrentSysTime(), currentTime, CurrentTempo);
    }
    return TRUE;
}

BOOL WINAPI Observer::BASS_ChannelSetAttribute(DWORD handle, DWORD attrib, float value)
{
    Observer *instance = Observer::GetInstance();
    if (!instance->hookerBASS_ChannelSetAttribute.GetFunction()(handle, attrib, value))
    {
        return FALSE;
    }

    BASS_CHANNELINFO info;
    BASS_ChannelGetInfo(handle, &info);

    if ((info.ctype & BASS_CTYPE_STREAM) && attrib == BASS_ATTRIB_TEMPO)
    {
        double currentTime = BASS_ChannelBytes2Seconds(handle, BASS_ChannelGetPosition(handle, BASS_POS_BYTE));
        instance->SendTempoInfomation(GetCurrentSysTime(), currentTime, value);
    }
    return TRUE;
}

BOOL WINAPI Observer::BASS_ChannelPause(DWORD handle)
{
    Observer *instance = Observer::GetInstance();
    if (!instance->hookerBASS_ChannelPause.GetFunction()(handle))
    {
        return FALSE;
    }

    BASS_CHANNELINFO info;
    BASS_ChannelGetInfo(handle, &info);
    if (info.ctype & BASS_CTYPE_STREAM)
    {
        double currentTime = BASS_ChannelBytes2Seconds(handle, BASS_ChannelGetPosition(handle, BASS_POS_BYTE));
        instance->SendTempoInfomation(GetCurrentSysTime(), currentTime, -100);
    }
    return TRUE;
}

BOOL WINAPI Observer::wglSwapBuffers(HDC context)
{
	Observer *instance = Observer::GetInstance();

	HWND hwnd;
	RECT rect;

	tstring lyrics = instance->GetLyrics();

	hwnd = WindowFromDC(context);
	GetClientRect(hwnd, &rect);

	int result = DrawTextW(context, lyrics.c_str(), lyrics.length(), &rect, DT_CENTER);

	if (!result) throw result;

	return instance->hooker_wglSwapBuffers.GetFunction()(context);
}

void Observer::SendTempoInfomation(long long calledAt, double currentTime, float tempo)
{
    TCHAR message[Server::nMessageLength];

    Observer *instance = Observer::GetInstance();

	/* Get Current Playing */
    EnterCriticalSection(&instance->hCritiaclSection);
    swprintf(message, L"%llx|%s|%lf|%f|%s\n", 
		calledAt, 
		instance->currentPlaying.audioPath.c_str(),
		currentTime, 
		tempo, 
		instance->currentPlaying.beatmapPath.c_str());
	LeaveCriticalSection(&instance->hCritiaclSection);

    Server::GetInstance()->PushMessage(message);
}

void Observer::Initalize()
{
    this->hookerReadFile.Hook();

    this->hookerBASS_ChannelPlay.Hook();
    this->hookerBASS_ChannelSetPosition.Hook();
    this->hookerBASS_ChannelSetAttribute.Hook();
    this->hookerBASS_ChannelPause.Hook();
	this->hooker_wglSwapBuffers.Hook();
}

void Observer::Release()
{
	this->hooker_wglSwapBuffers.Unhook();

    this->hookerBASS_ChannelPause.Unhook();
    this->hookerBASS_ChannelSetAttribute.Unhook();
    this->hookerBASS_ChannelSetPosition.Unhook();
    this->hookerBASS_ChannelPlay.Unhook();

    this->hookerReadFile.Unhook();
}
