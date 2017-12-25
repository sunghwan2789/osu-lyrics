using osu_Lyrics.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace osu_Lyrics.Lyrics.Sources
{
    public abstract class LyricSource
    {
        public static readonly List<LyricSource> sources = new List<LyricSource>();

        static LyricSource()
        {
            AlsongSource.Register();
        }

        protected static void AddSource<T>() where T : LyricSource
        {
            sources.Add((LyricSource) Activator.CreateInstance(typeof(T)));
        }

        public Lyric GetLyric(AudioInfo audio)
        {
            return GetLyricAsync(audio).GetAwaiter().GetResult();
        }

        public abstract Task<Lyric> GetLyricAsync(AudioInfo audio);
    }
}
