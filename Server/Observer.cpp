#pragma comment(lib, "Shlwapi.lib")

#pragma warning(disable:4996)

#include "Observer.h"

#include <cstdio>

#include <Windows.h>
#include <Shlwapi.h>
#include "bass.h"
#include "bass_fx.h"
#include "Hooker.h"
#include "Server.h"

Observer InstanceObserver;

#define AUDIO_FILE_INFO_TOKEN "AudioFilename:"
#define AUDIO_FILE_INFO_TOKEN_LENGTH 14

inline wchar_t* CutStringFromFront(wchar_t* string, unsigned int index)
{
    return (string + index);
}

BOOL WINAPI proxyReadFile(HANDLE hFile, LPVOID lpBuffer, DWORD nNumberOfBytesToRead, LPDWORD lpNumberOfBytesRead, LPOVERLAPPED lpOverlapped)
{
    if (!InstanceObserver.hookerReadFile.GetOriginalFunction()(hFile, lpBuffer, nNumberOfBytesToRead, lpNumberOfBytesRead, lpOverlapped))
    {
        return FALSE;
    }

    TCHAR nameTmpFilePath[MAX_PATH];

    DWORD dwTmpFilePathLength = GetFinalPathNameByHandle(hFile, nameTmpFilePath, MAX_PATH, VOLUME_NAME_DOS);
    DWORD dwFilePosition = SetFilePointer(hFile, 0, NULL, FILE_CURRENT) - (*lpNumberOfBytesRead);

    TCHAR* nameFile = CutStringFromFront(nameTmpFilePath, 4);
    DWORD  dwFilePathLength = dwTmpFilePathLength - 4;

    // 지금 읽는 파일이 비트맵 파일이고 앞부분을 읽었다면 음악 파일 경로 얻기:
    // 파일 이름이 포함된 Path 끝부분 4글자를 자름. 4글자와  .osu를 비교하여 이 파일이 osu 파일인지 확인함
    if (wcsncmp(L".osu", &nameFile[dwFilePathLength - 4], 4) == 0 && dwFilePosition == 0)
    {
        // .osu 파일은 UTF-8(Multibyte) 인코딩
        /* strtok은 소스를 변형하므로 먼저 strdup를 이용해서 글자를 복사함. */
        LPSTR buffer = strdup((const char*)(lpBuffer));

        /* for문을 이용해서 줄마다 strtok으로 잘라냄. */
		for (LPSTR line = strtok(buffer, "\n"); line != NULL; line = strtok(NULL, "\n"))
        {
            /* 잘라낸 줄에 Token이 있는지 확인하고 아닐경우에는 continue함. */
            if (strnicmp(line, AUDIO_FILE_INFO_TOKEN, AUDIO_FILE_INFO_TOKEN_LENGTH) != 0)
            {
                continue;
            }

            TCHAR nameAudioFile[MAX_PATH];
            mbstowcs(nameAudioFile, &line[AUDIO_FILE_INFO_TOKEN_LENGTH], MAX_PATH);
            StrTrimW(nameAudioFile, L" \r");

            TCHAR pathAudioFile[MAX_PATH];
            wcscpy(pathAudioFile, nameFile);
            /* pathAudioFile에서 파일명을 지웁니다. */
            PathRemoveFileSpecW(pathAudioFile);
            /* 파일명이 지워진 Path인 pathAudioFile에 nameAudioFile붙여, 완전한 Path를 만듭니다. */
            PathCombineW(pathAudioFile, pathAudioFile, nameAudioFile);

			EnterCriticalSection(&InstanceObserver.hCritiaclSection);
            InstanceObserver.currentPlaying.beatmapPath = (std::wstring(nameFile));
			InstanceObserver.currentPlaying.audioPath = (std::wstring(pathAudioFile));
            /* 똑같은 AudioFile 이름이 있다면 List에 추가하지 않는다. */
            for (auto info = InstanceObserver.listBeatmapCached.begin(); info != InstanceObserver.listBeatmapCached.end(); ++info)
            {
                if (!StrCmpW(pathAudioFile, info->audioPath.c_str()))
                {
                    goto ALREADY_CACHED;
                }
            }
            InstanceObserver.listBeatmapCached.push_back(InstanceObserver.currentPlaying);
        ALREADY_CACHED:

			LeaveCriticalSection(&InstanceObserver.hCritiaclSection);

            break;
        }

        /* strdup를 이용해 복사한 문자열의 메모리를 해제시킵니다. */
        free(buffer);
    }
    else
    {
        /* Beatmap을 다시 불러오지 않고 Cache에서 불러올때가 있는데.                          */
        /* 그럴경우에는 Audio파일만 읽으니 동일한 Audio파일이 기존에 읽힌적이 있는지 확인하고 */
        /* 현제 실행되고있는 곡을 지금 읽은 AudioFile으로 바꾼다.                             */
        EnterCriticalSection(&InstanceObserver.hCritiaclSection);
        for (auto info = InstanceObserver.listBeatmapCached.begin(); info != InstanceObserver.listBeatmapCached.end(); ++info)
        {
            /* 같은 AudioFile인지 비교한다. */
            if (!StrCmpW(nameFile, info->audioPath.c_str()))
            {
                InstanceObserver.currentPlaying.beatmapPath = (info->beatmapPath);
                InstanceObserver.currentPlaying.audioPath = (info->audioPath);
                break;
            }
        }
        LeaveCriticalSection(&InstanceObserver.hCritiaclSection);
    }
    return TRUE;
}

/* 현제 시스템 시간을 구합니다. */
inline long long GetCurrentSysTime()
{
    long long time;

    GetSystemTimeAsFileTime((LPFILETIME)&time); return time;
}

BOOL WINAPI proxyBASS_ChannelPlay(DWORD handle, BOOL restart)
{
    if (!InstanceObserver.hookerBASS_ChannelPlay.GetOriginalFunction()(handle, restart))
    {
        return FALSE;
    }

    BASS_CHANNELINFO info;
    BASS_ChannelGetInfo(handle, &info);

    if (info.ctype & BASS_CTYPE_STREAM)
    {
        double currentTimePos = BASS_ChannelBytes2Seconds(handle, BASS_ChannelGetPosition(handle, BASS_POS_BYTE));
        float tempo = 0; 
        BASS_ChannelGetAttribute(handle, BASS_ATTRIB_TEMPO, &tempo);
        InstanceObserver.SendInfomation(GetCurrentSysTime(), currentTimePos, tempo);
    }
    return TRUE;
}

BOOL WINAPI proxyBASS_ChannelSetPosition(DWORD handle, QWORD pos, DWORD mode)
{
    if (!InstanceObserver.hookerBASS_ChannelSetPosition.GetOriginalFunction()(handle, pos, mode))
    {
        return FALSE;
    }

    BASS_CHANNELINFO info;
    BASS_ChannelGetInfo(handle, &info);

    if (info.ctype & BASS_CTYPE_STREAM)
    {
        double currentTime = BASS_ChannelBytes2Seconds(handle, pos);
        float CurrentTempo = 0; 
        BASS_ChannelGetAttribute(handle, BASS_ATTRIB_TEMPO, &CurrentTempo);
        // 주의!! pos가 일정 이하일 때,
        // 재생하면 proxyBASS_ChannelPlay대신 이 함수가 호출되고,
        // BASS_ChannelIsActive 값은 BASS_ACTIVE_PAUSED임.
        if (BASS_ChannelIsActive(handle) == BASS_ACTIVE_PAUSED)
        {
            CurrentTempo = -100;
        }

        InstanceObserver.SendInfomation(GetCurrentSysTime(), currentTime, CurrentTempo);
    }
    return TRUE;
}

BOOL WINAPI proxyBASS_ChannelSetAttribute(DWORD handle, DWORD attrib, float value)
{
    if (!InstanceObserver.hookerBASS_ChannelSetAttribute.GetOriginalFunction()(handle, attrib, value))
    {
        return FALSE;
    }

    BASS_CHANNELINFO info;
    BASS_ChannelGetInfo(handle, &info);

    if ((info.ctype & BASS_CTYPE_STREAM) && attrib == BASS_ATTRIB_TEMPO)
    {
        double currentTime = BASS_ChannelBytes2Seconds(handle, BASS_ChannelGetPosition(handle, BASS_POS_BYTE));
        InstanceObserver.SendInfomation(GetCurrentSysTime(), currentTime, value);
    }
    return TRUE;
}

BOOL WINAPI proxyBASS_ChannelPause(DWORD handle)
{
    if (!InstanceObserver.hookerBASS_ChannelPause.GetOriginalFunction()(handle))
    {
        return FALSE;
    }

    BASS_CHANNELINFO info;
    BASS_ChannelGetInfo(handle, &info);
    if (info.ctype & BASS_CTYPE_STREAM)
    {
        double currentTime = BASS_ChannelBytes2Seconds(handle, BASS_ChannelGetPosition(handle, BASS_POS_BYTE));
        InstanceObserver.SendInfomation(GetCurrentSysTime(), currentTime, -100);
    }
    return TRUE;
}

void Observer::SendInfomation(long long calledAt, double currentTime, float tempo)
{
    TCHAR message[Server::MAX_MESSAGE_LENGTH];

	/* 지금 무엇을 플레이하고있는지 proxyReadFile 에서 얻어낸 값을 클라이언트로 전송합니다. */
    EnterCriticalSection(&hCritiaclSection);
    swprintf(message, L"%llx|%s|%lf|%f|%s\n", 
		calledAt, 
		currentPlaying.audioPath.c_str(),
		currentTime, 
		tempo, 
		currentPlaying.beatmapPath.c_str());
	LeaveCriticalSection(&hCritiaclSection);

    GetServerInstance()->PushMessage(message);
}

void Observer::Start()
{
	DetourTransactionBegin();
	DetourUpdateThread(GetCurrentThread());

    this->hookerReadFile.Hook();
    this->hookerBASS_ChannelPlay.Hook();
    this->hookerBASS_ChannelSetPosition.Hook();
    this->hookerBASS_ChannelSetAttribute.Hook();
    this->hookerBASS_ChannelPause.Hook();

	DetourTransactionCommit();
}

void Observer::Stop()
{
	DetourTransactionBegin();
	DetourUpdateThread(GetCurrentThread());

    this->hookerBASS_ChannelPause.Unhook();
    this->hookerBASS_ChannelSetAttribute.Unhook();
    this->hookerBASS_ChannelSetPosition.Unhook();
    this->hookerBASS_ChannelPlay.Unhook();
    this->hookerReadFile.Unhook();

	DetourTransactionCommit();
}
