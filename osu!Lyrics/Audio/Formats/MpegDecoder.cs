using System;
using System.IO;

namespace osu_Lyrics.Audio.Formats
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
                    s.Seek(-read, SeekOrigin.Current);
                    break;
                }

                // ID3v2 태그 건너뛰기
                s.Seek(buff[6] << 21 | buff[7] << 14 | buff[8] << 7 | buff[9], SeekOrigin.Current);
            } while (read > 0);
        }

        private static void SeekHeader(Stream s)
        {
            var buff = new byte[4];
            int read;
            do
            {
                read = s.Read(buff, 0, buff.Length);

                // MPEG 헤더를 찾았을 경우
                if (Validate(Program.IntB(buff, 0, 4)))
                {
                    s.Seek(-read, SeekOrigin.Current);
                    break;
                }
                
                // 패딩 건너뛰기
                s.Seek(-read + 1, SeekOrigin.Current);
            } while (read > 0);
        }

        public MpegDecoder() { }

        private static bool Validate(int header)
        {
            const int SYNC = 0b1111_1111_111;
            const int VERSION_RESERVED = 0b01;
            const int LAYER_RESERVED = 0b00;
            const int BITRATE_BAD = 0b1111;
            const int SAMPLERATE_RESERVED = 0b11;
            const int EMPHASIS_RESERVED = 0b10;
            return ParseSync(header) == SYNC
                && ParseVersion(header) != VERSION_RESERVED
                && ParseLayer(header) != LAYER_RESERVED
                && ParseBitRate(header) != BITRATE_BAD
                && ParseSampleRate(header) != SAMPLERATE_RESERVED
                && ParseEmphasis(header) != EMPHASIS_RESERVED;
        }
        
        private static int ParseSync(int header) => (header >> 21) & 0x7FF;
        private static int ParseVersion(int header) => (header >> 19) & 3;
        private static int ParseLayer(int header) => (header >> 17) & 3;
        private static int ParseBitRate(int header) => (header >> 12) & 0xF;
        private static int ParseSampleRate(int header) => (header >> 10) & 3;
        private static int ParseEmphasis(int header) => header & 3;

        protected override void ParseFile(Stream stream, AudioInfo audio)
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