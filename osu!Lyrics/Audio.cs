namespace osu_Lyrics
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
                this.Hash = AudioHash.Load(path);
            }
            catch {}
            try
            {
                this.Beatmap = Beatmap.Load(beatmapPath);
            }
            catch {}
        }
    }
}
