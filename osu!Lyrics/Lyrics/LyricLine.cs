using System;

namespace osu_Lyrics.Lyrics
{
    internal class LyricLine
    {
        public double Time { get; private set; }
        public string Text { get; private set; }

        public LyricLine() { }

        public LyricLine(string data)
        {
            // [0:2.4]가사@
            var lyric = data.Split(new[] { ']' }, 2);
            var time = lyric[0].TrimStart('[').Split(':');
            Time = double.Parse(time[0]) * 60 + double.Parse(time[1]);
            Text = lyric.Length > 1 ? lyric[1].Trim() : string.Empty;
        }

        public LyricLine(double time, string text)
        {
            Time = time;
            Text = text;
        }
    }
}