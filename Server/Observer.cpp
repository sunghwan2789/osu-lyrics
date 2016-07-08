#pragma comment(lib, "Shlwapi.lib")
#pragma comment(lib, "opengl32.lib")

#pragma warning(disable:4996)
#pragma warning(disable:4244)

#include "Observer.h"

#include <cstdio>
#include <tchar.h>
#include <string>

#include <gl/GL.h>
#include <gl/GLU.h>

#include <Windows.h>
#include <Shlwapi.h>
#include "bass.h"
#include "bass_fx.h"
#include "Hooker.h"
#include "Server.h"

#include <ft2build.h>
#include <freetype/freetype.h>
#include <freetype/ftglyph.h>
#include <freetype/ftoutln.h>
#include <freetype/fttrigon.h>

#define AUDIO_FILE_INFO_TOKEN "AudioFilename:"

std::shared_ptr<Observer> Observer::instance;
std::once_flag Observer::once_flag;

BOOL WINAPI Observer::ReadFile(HANDLE hFile, LPVOID lpBuffer, DWORD nNumberOfBytesToRead, LPDWORD lpNumberOfBytesRead, LPOVERLAPPED lpOverlapped)
{
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

#define UNICODE_FONTS (256 * 256)
#define FONT_NAME     ("C:/Windows/Fonts/H2PORL.TTF")

GLbyte     fontColor[3] = { 50, 100, 50 };
GLuint     fontListBase;
GLuint     fontTextures[UNICODE_FONTS];

GLdouble   fontPosition[2] = { -1.0f, 0.0f };

bool       isFontInitalized = false;

FT_Library ft_library;
FT_Face    ft_face;

inline int next_p2(int a)
{
	int rval = 1;

	while (rval < a) rval <<= 1;
	return rval;
}

BOOL WINAPI Observer::wglSwapBuffers(HDC context)
{
	if (!isFontInitalized)
	{
		FT_Init_FreeType(&ft_library);
		FT_New_Face(ft_library, FONT_NAME, 0, &ft_face);
		FT_Set_Char_Size(ft_face, 0, 16 * 16, 96, 96);


		fontListBase = glGenLists(UNICODE_FONTS);
		glGenTextures(UNICODE_FONTS, fontTextures);

		for (unsigned int i = 0; i < UNICODE_FONTS; i++)
		{
			FT_Glyph       glyph;
			FT_BitmapGlyph glyphBitmap;

			GLubyte*       expanded_data;

			FT_Load_Glyph(ft_face, FT_Get_Char_Index(ft_face, i), FT_LOAD_DEFAULT);
			FT_Get_Glyph(ft_face->glyph, &glyph);

			FT_Glyph_To_Bitmap(&glyph, ft_render_mode_normal, 0, 1);

			glyphBitmap = (FT_BitmapGlyph)glyph;
			FT_Bitmap& bitmap = glyphBitmap->bitmap;

			uint32_t width = next_p2(bitmap.width);
			uint32_t height = next_p2(bitmap.rows);

			expanded_data = new GLubyte[2 * width * height];

			for (unsigned int j = 0; j < height; j++) {
				for (unsigned int i = 0; i < width; i++) {
					expanded_data[2 * (i + j * width)] = 255;
					expanded_data[2 * (i + j * width) + 1] =
						(i >= bitmap.width || j >= bitmap.rows) ? 0 : bitmap.buffer[i + bitmap.width * j];
				}
			}

			delete[] expanded_data;
			
			glNewList(fontListBase + i, GL_COMPILE);
			glBindTexture(GL_TEXTURE_2D, fontTextures[i]);
			glPushMatrix();
			glTranslatef(glyphBitmap->left, 0, 0);
			glTranslatef(0, glyphBitmap->top - bitmap.rows, 0);

			float   x = (float)bitmap.width / (float)width,
					y = (float)bitmap.rows / (float)height;

			glBegin(GL_QUADS);
			glTexCoord2d(0, 0); glVertex2f(0, bitmap.rows);
			glTexCoord2d(0, y); glVertex2f(0, 0);
			glTexCoord2d(x, y); glVertex2f(bitmap.width, 0);
			glTexCoord2d(x, 0); glVertex2f(bitmap.width, bitmap.rows);
			glEnd();
			glPopMatrix();
			glTranslatef(ft_face->glyph->advance.x >> 6, 0, 0);

			glEndList();

			FT_Done_Glyph(glyph);
		}

		FT_Done_Face(ft_face);
		FT_Done_FreeType(ft_library);

		isFontInitalized = true;
	}

	glRasterPos2dv(fontPosition);
	glColor3bv(fontColor);

	glPushAttrib(GL_LIST_BIT);
	glListBase(fontListBase);
	glCallLists(
		GLsizei(instance->lyrics.length()),
		GL_UNSIGNED_SHORT,
		instance->lyrics.c_str());
	glPopAttrib();

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
	this->lyrics = tstring(L"가나다라마바사TESTㄱㄴㄷㄹ＠＆キャラメルへヴン 無人");

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
