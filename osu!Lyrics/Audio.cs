using System.IO;

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
                Info = Program.IntB(buff, 0, fs.Read(buff, 0, 4)) == 0x4F676753 // "OggS"
                    ? (AudioInfo) new Ogg(fs)
                    : (AudioInfo) new Mpeg(fs);
            }
        }
    }
}