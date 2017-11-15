using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace osu_Lyrics.IO
{
    internal class AudioDecoder
    {
        protected long RawPosition = 0;

        public string Hash { get; protected set; }

        protected void SetHash(Stream s)
        {
            s.Seek(RawPosition, SeekOrigin.Begin);
            var buff = new byte[0x28000];
            var read = s.Read(buff, 0, buff.Length);
            this.Hash = string.Join("", MD5.Create().ComputeHash(buff, 0, read).Select(i => i.ToString("x2")));
        }

        public static string Load(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                var buff = new byte[4];
                var mediaType = Program.IntB(buff, 0, fs.Read(buff, 0, 4)) == 0x4F676753 ? // "OggS"
                    (AudioDecoder) new Formats.Ogg(fs) :
                    (AudioDecoder) new Formats.Mpeg(fs);
                return mediaType.Hash;
            }
        }
    }
}
