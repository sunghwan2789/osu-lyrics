using osu_Lyrics.Formats;
using System.IO;

namespace osu_Lyrics.Models
{
    internal class Audio
    {
        public readonly string Path;
        public readonly string Hash;
        public readonly Beatmap Beatmap;

        public double Sync;

        public Audio()
        {
            this.Sync = 0;
        }

        public Audio(string path, string beatmapPath) : this()
        {
            this.Path = path;
            try
            {
                this.Hash = IO.AudioDecoder.Load(path);
            }
            catch {}
            try
            {
                using (var sr = new StreamReader(beatmapPath))
                {
                    this.Beatmap = BeatmapDecoder.GetDecoder(sr)?.Decode(sr);
                }
            }
            catch {}
        }
    }
}
