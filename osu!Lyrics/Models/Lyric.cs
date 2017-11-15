using System;

namespace osu_Lyrics.Models
{
    internal class Lyric
    {
        public double Time { get; private set; }
        public string Text { get; private set; }

        public Lyric(string data)
        {
            // [0:2.4]가사@
            var lyric = data.Split(new[] { ']' }, 2);
            var time = lyric[0].Substring(1).Split(':');
            Time = Convert.ToDouble(time[0]) * 60 + Convert.ToDouble(time[1]);
            Text = lyric[1].Trim();
        }

        public Lyric()
        {
            Text = "";
        }

        public Lyric(double time, string text)
        {
            Time = time;
            Text = text;
        }
    }
}