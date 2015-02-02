using System;
using System.IO;
using System.Text;

namespace osu_Lyrics
{
    internal class Audio
    {
        public string Path { get; private set; }

        public AudioInfo Info { get; private set; }

        public double Sync { get; set; }

        public string Beatmap { get; set; }

        public Audio() {}

        public Audio(string path)
        {
            Path = path;

            using (var fs = new FileStream(Osu.Directory + Path, FileMode.Open, FileAccess.Read))
            {
                var buff = new byte[4];
                Info = "OggS".Equals(
                    Encoding.ASCII.GetString(buff, 0, fs.Read(buff, 0, buff.Length)), StringComparison.Ordinal)
                    ? (AudioInfo) new Ogg(fs)
                    : (AudioInfo) new Mpeg(fs);
            }
        }
    }
}