using System;
using System.IO;
using osu_Lyrics.Models;

namespace osu_Lyrics.Formats
{
    internal class MpegDecoder : AudioDecoder
    {
        public static void Register()
        {
            AddDecoder<MpegDecoder>(TypeCheck);
        }

        public static bool TypeCheck(Stream s)
        {
            s.Seek(0, SeekOrigin.Begin);

            SkipID3v2(s);
            SeekHeader(s);

            return s.Position < s.Length - 1;
        }

        private static void SkipID3v2(Stream s)
        {
            const int IDENTIFIER = 0x494433;

            var buff = new byte[10];
            int read;
            do
            {
                read = s.Read(buff, 0, buff.Length);

                // 더이상 ID3v2 태그가 없는 경우
                if (Program.IntB(buff, 0, 3) != IDENTIFIER)
                {
                    s.Seek(-buff.Length, SeekOrigin.Current);
                    break;
                }

                // ID3v2 태그 건너뛰기
                s.Seek(buff[6] << 21 | buff[7] << 14 | buff[8] << 7 | buff[9], SeekOrigin.Current);
            } while (read > 0);
        }

        private static void SeekHeader(Stream s)
        {
            const int FRAME_SYNC = 0xFFE0F0;

            var buff = new byte[3];
            int read;
            do
            {
                read = s.Read(buff, 0, buff.Length);

                // MPEG 헤더를 찾았을 경우
                if ((Program.IntB(buff, 0, 3) & FRAME_SYNC) == FRAME_SYNC)
                {
                    s.Seek(-buff.Length, SeekOrigin.Current);
                    break;
                }
                
                // 패딩 건너뛰기
                s.Seek(-buff.Length + 1, SeekOrigin.Current);
            } while (read > 0);
        }

        public MpegDecoder() { }

        protected override void ParseFile(Stream stream, Audio audio)
        {
            if (audio == null)
            {
                throw new ArgumentNullException(nameof(audio));
            }
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            stream.Seek(0, SeekOrigin.Begin);

            SkipID3v2(stream);
            SeekHeader(stream);

            audio.CheckSum = GetCheckSum(stream);
        }
    }
}