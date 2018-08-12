using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using osu_Lyrics.Audio;

namespace osu_Lyrics.Lyrics.Sources
{
    class AlsongGetLyric5 : AlsongSource
    {
        public static void Register()
        {
            AddSource<AlsongGetLyric5>();
        }

        public override Task<Lyric> GetLyricAsync(AudioInfo audio)
        {
            var data = Properties.Resources.ResourceManager.GetString("GetLyric5");
            data = data.Replace("[HASH]", audio.CheckSum);
            return GetLyricAsync("GetLyric5", data);
        }
    }
}
