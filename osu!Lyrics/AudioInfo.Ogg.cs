using System;
using System.IO;
using System.Text;

namespace osu_Lyrics
{
    internal class Ogg : AudioInfo
    {
        public Ogg(Stream s)
        {
            s.Seek(48, SeekOrigin.Begin);

            // identification header
            var buff = new byte[4];
            s.Read(buff, 0, buff.Length);
            BitRate = buff[0] | buff[1] << 8 | buff[2] << 16 | buff[3] << 24;

            // setup header
            buff = new byte[4096];
            int read;
            do
            {
                read = s.Read(buff, 0, buff.Length);
                for (var i = 11; i < read; i++)
                {
                    if (buff[i - 11] == 0x05 &&
                        "vorbis".Equals(Encoding.ASCII.GetString(buff, i - 10, 6), StringComparison.Ordinal) &&
                        // codebook?
                        "BCV".Equals(Encoding.ASCII.GetString(buff, i - 3, 3), StringComparison.Ordinal))
                    {
                        s.Seek(-read + i + 12, SeekOrigin.Current);
                        read = 0;
                        break;
                    }
                }
                s.Seek(-12, SeekOrigin.Current);
            } while (read == buff.Length);

            RawPosition = s.Position;
            SetHash(s);
        }
    }
}