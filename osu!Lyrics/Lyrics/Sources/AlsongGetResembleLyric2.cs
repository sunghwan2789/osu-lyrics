using osu_Lyrics.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace osu_Lyrics.Lyrics.Sources
{
    internal class AlsongGetResembleLyric2 : AlsongSource
    {
        public static void Register()
        {
            AddSource<AlsongGetResembleLyric2>();
        }

        public override Task<Lyric> GetLyricAsync(AudioInfo audio) =>
            GetLyricAsync("GetResembleLyric2", $@"<?xml version='1.0' encoding='UTF-8'?>
<Envelope xmlns='http://www.w3.org/2003/05/soap-envelope'>
    <Body>
        <GetResembleLyric2 xmlns='ALSongWebServer'>
            <stQuery>
                <strTitle>{audio.Beatmap.TitleUnicode ?? audio.Beatmap.Title}</strTitle>
                <strArtistName>{audio.Beatmap.ArtistUnicode ?? audio.Beatmap.Artist}</strArtistName>
            </stQuery>
        </GetResembleLyric2>
    </Body>
</Envelope>");
    }
}