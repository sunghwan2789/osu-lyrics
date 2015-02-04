using System.IO;

namespace osu_Lyrics
{
    internal class Ogg : AudioInfo
    {
        public Ogg(Stream s)
        {
            s.Seek(48, SeekOrigin.Begin);

            // identification header
            var buff = new byte[4096];
            BitRate = Program.Int(buff, 0, s.Read(buff, 0, 4));

            // setup header
            int read;
            do
            {
                read = s.Read(buff, 0, buff.Length);
                for (var i = 11; i < read; i++)
                {
                    if (Validate(buff, i - 11))
                    {
                        RawPosition = s.Position - read + i;
                        read = 0;
                        break;
                    }
                }
                s.Seek(-12, SeekOrigin.Current);
            } while (read == buff.Length);

            SetHash(s);
        }

        private static bool Validate(byte[] buff, int offset)
        {
            return buff[offset] == 5 && Program.LongB(buff, offset + 1, 6) == 0x766F72626973 && // "vorbis"
                   Program.IntB(buff, offset + 8, 3) == 0x424356; // "BCV" codebook?
        }
    }
}