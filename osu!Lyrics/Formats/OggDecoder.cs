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

        public OggDecoder() { }

        private static bool Validate(byte[] buff, int offset)
        {
            return buff[offset] == 5 && Program.LongB(buff, offset + 1, 6) == 0x766F72626973 && // "vorbis"
                   Program.IntB(buff, offset + 8, 3) == 0x424356; // "BCV" codebook?
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

            stream.Seek(52, SeekOrigin.Begin);

            var buff = new byte[4096];
            int read;
            do
            {
                read = stream.Read(buff, 0, buff.Length);

                for (var i = 11; i < read; i++)
                {
                    // Setup 헤더를 찾았을 경우
                    if (Validate(buff, i - 11))
                    {
                        stream.Seek(-read + i, SeekOrigin.Current);
                        break;
                    }
                }

                // 패딩 건너뛰기
                stream.Seek(-12, SeekOrigin.Current);
            } while (read == buff.Length);

            audio.CheckSum = GetCheckSum(stream);
        }
    }
}