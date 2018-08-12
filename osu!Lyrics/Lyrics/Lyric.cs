using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace osu_Lyrics.Lyrics
{
    public class Lyric : List<LyricLine>
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public double Sync { get; set; }

        public Lyric() { }

        public Lyric(string[] data)
        {
            AddRange(data.Select(i => new LyricLine(i)));
        }

        public Lyric(string line)
        {
            Add(new LyricLine(0, line));
        }

        public IEnumerator<LyricLine> EnumeratorAt(double time) => this.SkipWhile(i => i.Time < time).GetEnumerator();
    }
}
