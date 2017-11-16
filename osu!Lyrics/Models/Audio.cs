using osu_Lyrics.Formats;
using System.IO;

namespace osu_Lyrics.Models
{
    internal class Audio
    {
        public string Path { get; set; }

        public string CheckSum { get; set; }
        public Beatmap Beatmap { get; set; }

        public double Sync;

        public Audio()
        {
            this.Sync = 0;
        }
    }
}
