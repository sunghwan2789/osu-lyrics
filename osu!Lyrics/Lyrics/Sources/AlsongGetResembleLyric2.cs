using osu_Lyrics.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace osu_Lyrics.Lyrics.Sources
{
    class AlsongGetResembleLyric2 : AlsongSource
    {
        public static void Register()
        {
            AddSource<AlsongGetResembleLyric2>();
        }

        public override Task<Lyric> GetLyricAsync(AudioInfo audio)
        {
            var data = Properties.Resources.ResourceManager.GetString("GetResembleLyric2");
            var title = audio.Beatmap.TitleUnicode ?? audio.Beatmap.Title;
            var artist = audio.Beatmap.ArtistUnicode ?? audio.Beatmap.Artist;
            data = data.Replace("[TITLE]", title).Replace("[ARTIST]", artist);
            return GetLyricAsync("GetResembleLyric2", data);
        }
    }
}
