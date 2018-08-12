namespace osu_Lyrics.Audio
{
    public class AudioInfo
    {
        public string Path { get; set; }

        public string CheckSum { get; set; }
        public Beatmap.BeatmapMetadata Beatmap { get; set; }

        public double Sync = 0;

        public AudioInfo() { }
    }
}
