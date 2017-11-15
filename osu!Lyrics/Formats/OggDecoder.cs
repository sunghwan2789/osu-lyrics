using System;
using System.IO;
using osu_Lyrics.Models;

namespace osu_Lyrics.Formats
{
    internal class OggDecoder : AudioDecoder
    {
        public static void Register()
        {
            AddDecoder<OggDecoder>(TypeCheck);
        }

        private static bool TypeCheck(Stream s)
        {
            s.Seek(0, SeekOrigin.Begin);

            var buff = new byte[4];
            return Program.IntB(buff, 0, s.Read(buff, 0, 4)) == 0x4F676753; // "OggS"
        }

        private static void SeekHeader(Stream s)
        {
            s.Seek(52, SeekOrigin.Begin);

            var buff = new byte[12];
            int read;
            do
            {
                read = s.Read(buff, 0, buff.Length);

                // Setup 헤더를 찾았을 경우
                if (Validate(buff))
                {
                    s.Seek(-read, SeekOrigin.Current);
                    break;
                }

                // 패딩 건너뛰기
                s.Seek(-read + 1, SeekOrigin.Current);
            } while (read > 0);
        }

        public OggDecoder() { }

        private static bool Validate(byte[] buff)
        {
            return buff[0] == 5 && Program.LongB(buff, 1, 6) == 0x766F72626973 // "vorbis"
                && Program.IntB(buff, 8, 3) == 0x424356; // "BCV" codebook?
        }

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

            SeekHeader(stream);

            audio.CheckSum = GetCheckSum(stream);
        }
    }
}