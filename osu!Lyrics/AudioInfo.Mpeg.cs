using System.IO;

namespace osu_Lyrics
{
    internal class Mpeg : AudioInfo
    {
        public Mpeg(Stream s)
        {
            s.Seek(0, SeekOrigin.Begin);

            // ID3
            var buff = new byte[10];
            int read;
            do
            {
                read = s.Read(buff, 0, buff.Length);
                if (Program.IntB(buff, 0, 3) == 0x494433) // "ID3"
                {
                    s.Seek(buff[6] << 21 | buff[7] << 14 | buff[8] << 7 | buff[9], SeekOrigin.Current);
                }
                else
                {
                    s.Seek(-buff.Length, SeekOrigin.Current);
                    break;
                }
            } while (read == buff.Length);

            // header
            buff = new byte[4096];
            do
            {
                read = s.Read(buff, 0, buff.Length);
                for (var i = 3; i < read; i++)
                {
                    var header = Program.IntB(buff, i - 3);
                    if (Validate(header))
                    {
                        RawPosition = s.Position - read + i - 3;
                        read = 0;

                        s.Seek(RawPosition + 4 + LoadStream(header), SeekOrigin.Begin);
                        s.Read(buff, 0, 12);
                        BitRate = GetBitRate(header, LoadVBR(buff), s.Length - RawPosition);
                        break;
                    }
                }
                s.Seek(-4, SeekOrigin.Current);
            } while (read == buff.Length);

            SetHash(s);
        }

        private static int LoadStream(int header)
        {
            return ParseVersion(header) == 3 ? (ParseMode(header) == 3 ? 17 : 32) : (ParseMode(header) == 3 ? 9 : 17);
        }

        private static int LoadVBR(byte[] buff)
        {
            return Program.IntB(buff, 0) == 0x58696E67 // "Xing"
                ? (Program.IntB(buff, 4) & 1) == 1 ? Program.IntB(buff, 8) : -1
                : 0;
        }

        private static bool Validate(int header)
        {
            return ((header >> 21) & 0x7FF) == 0x7FF && ParseVersion(header) != 1 && ParseLayer(header) != 0 &&
                   ParseBitRate(header) != 0 && ParseBitRate(header) != 0xF && ParseSampleRate(header) != 3 &&
                   ParseEmphasis(header) != 2;
        }

        private static int ParseVersion(int header)
        {
            return (header >> 19) & 3;
        }

        private static int ParseLayer(int header)
        {
            return (header >> 17) & 3;
        }

        private static int ParseBitRate(int header)
        {
            return (header >> 12) & 0xF;
        }

        private static int ParseSampleRate(int header)
        {
            return (header >> 10) & 3;
        }

        private static int ParseMode(int header)
        {
            return (header >> 6) & 3;
        }

        private static int ParseEmphasis(int header)
        {
            return header & 3;
        }

        private static int GetBitRate(int header, int vframes, long flen)
        {
            if (vframes != 0)
            {
                return (int) (flen / vframes * GetSampleRate(header) / (ParseLayer(header) == 3 ? 12.0 : 144.0));
            }
            var table = new[]
            {
                new[] { 0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160, 0 },
                new[] { 0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160, 0 },
                new[] { 0, 32, 48, 56, 64, 80, 96, 112, 128, 144, 160, 176, 192, 224, 256, 0 },
                new[] { 0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 0 },
                new[] { 0, 32, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 384, 0 },
                new[] { 0, 32, 64, 96, 128, 160, 192, 224, 256, 288, 320, 352, 384, 416, 448, 0 }
            };
            return table[(ParseVersion(header) & 1) * 3 + ParseLayer(header) - 1][ParseBitRate(header)] * 1000;
        }

        private static int GetSampleRate(int header)
        {
            var table = new[]
            {
                new[] { 32000, 16000, 8000 }, new[] { 0, 0, 0 }, new[] { 22050, 24000, 16000 },
                new[] { 44100, 48000, 32000 }
            };
            return table[ParseVersion(header)][ParseSampleRate(header)];
        }
    }
}