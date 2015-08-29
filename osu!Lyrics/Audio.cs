namespace osu_Lyrics
{
    internal class Audio
    {
        public string Path { get; private set; }

        public AudioInfo Info { get; private set; }

        public double Sync { get; set; }

        public Beatmap Beatmap { get; set; }

        public Audio() {}

        public Audio(string path, string beatmapPath)
        {
            Path = path;
            Info = AudioInfo.Parse(Path);
            Beatmap = new Beatmap(beatmapPath);
        }
    }
}