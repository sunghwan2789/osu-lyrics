using System.IO;

namespace osu_Lyrics.Formats
{
    internal class Mpeg : IO.AudioDecoder
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
                    }
                }
                s.Seek(-4, SeekOrigin.Current);
            } while (read == buff.Length);

            SetHash(s);
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
    }
}