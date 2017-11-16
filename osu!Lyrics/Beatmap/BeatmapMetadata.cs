namespace osu_Lyrics.Beatmap
{
    internal class BeatmapMetadata
    {
        public string Title { get; set; }
        public string TitleUnicode { get; set; }
        public string Artist { get; set; }
        public string ArtistUnicode { get; set; }
        public string AuthorString { get; set; }
        public string Source { get; set; }
        public string Tags { get; set; }
        public int PreviewTime { get; set; }
        public string AudioFile { get; set; }
        public string BackgroundFile { get; set; }

        public BeatmapMetadata(BeatmapMetadata original = null)
        {
            if (original == null)
            {
                Title = @"Unknown";
                Artist = @"Unknown";
                AuthorString = @"Unknown Creator";
            }
        }
    }
}
