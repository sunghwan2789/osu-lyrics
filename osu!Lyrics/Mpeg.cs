using System;
using System.IO;
using System.Text;

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
                if ("ID3".Equals(Encoding.ASCII.GetString(buff, 0, 3), StringComparison.Ordinal))
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
                    var header = buff[i - 3] << 24 | buff[i - 2] << 16 | buff[i - 1] << 8 | buff[i];
                    if (Validate(header))
                    {
                        KBitrate =
                            BitrateTable[(ParseVersion(header) & 1) * 3 + ParseLayer(header) - 1][ParseBitrate(header)] *
                            1000;
                        s.Seek(-read + i + 1, SeekOrigin.Current);
                        read = 0;
                        break;
                    }
                }
                s.Seek(-4, SeekOrigin.Current);
            } while (read == buff.Length);

            RawPosition = s.Position;
            SetHash(s);
        }


        private static bool Validate(int header)
        {
            return ((header >> 21) & 0x07FF) == 0x07FF && ParseVersion(header) != 0x01 && ParseLayer(header) != 0x00 &&
                   ParseBitrate(header) != 0x00 && ParseBitrate(header) != 0x0F && ParseFrequency(header) != 0x03 &&
                   ParseEmphasis(header) != 0x02;
        }

        private static int ParseVersion(int header)
        {
            return (header >> 19) & 0x03;
        }

        private static int ParseLayer(int header)
        {
            return (header >> 17) & 0x03;
        }

        private static int ParseBitrate(int header)
        {
            return (header >> 12) & 0x0F;
        }

        private static int ParseFrequency(int header)
        {
            return (header >> 10) & 0x03;
        }

        private static int ParseEmphasis(int header)
        {
            return header & 0x03;
        }

        private static readonly int[][] BitrateTable =
        {
            new[] { 0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160, 0 },
            new[] { 0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160, 0 },
            new[] { 0, 32, 48, 56, 64, 80, 96, 112, 128, 144, 160, 176, 192, 224, 256, 0 },
            new[] { 0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 0 },
            new[] { 0, 32, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 384, 0 },
            new[] { 0, 32, 64, 96, 128, 160, 192, 224, 256, 288, 320, 352, 384, 416, 448, 0 }
        };
    }
}