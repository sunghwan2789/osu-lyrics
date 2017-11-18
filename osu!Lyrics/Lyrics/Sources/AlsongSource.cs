using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using osu_Lyrics.Audio;
using System.IO;

namespace osu_Lyrics.Lyrics.Sources
{
    class AlsongSource : LyricSource
    {
        public static void Register()
        {
            AddSource<AlsongSource>();
        }

        public override Task<Lyric> GetLyricAsync(AudioInfo audio)
        {
            return GetLyric5Async(audio.CheckSum)
                ?? GetResembleLyric2Async(
                    audio.Beatmap.TitleUnicode ?? audio.Beatmap.Title,
                    audio.Beatmap.ArtistUnicode ?? audio.Beatmap.ArtistUnicode);
        }

        public Task<Lyric> GetLyric5Async(string checkSum)
        {
            var data = Properties.Resources.ResourceManager.GetString("GetLyric5");
            data = data.Replace("[HASH]", checkSum);
            return GetResponseAsync("GetLyric5", data);
        }

        public Task<Lyric> GetResembleLyric2Async(string title, string artist)
        {
            var data = Properties.Resources.ResourceManager.GetString("GetResembleLyric2");
            data = data.Replace("[TITLE]", title).Replace("[ARTIST]", artist);
            return GetResponseAsync("GetResembleLyric2", data);
        }

        private async Task<Lyric> GetResponseAsync(string action, string data)
        {
            try
            {
                var wr = Request.Create(@"http://lyrics.alsong.co.kr/alsongwebservice/service1.asmx");
                wr.Method = "POST";
                wr.UserAgent = "gSOAP";
                wr.ContentType = "application/soap+xml; charset=UTF-8";
                wr.Headers.Add("SOAPAction", "ALSongWebServer/" + action);

                using (var rq = new StreamWriter(wr.GetRequestStream()))
                {
                    rq.Write(data);
                }

                using (var rp = new StreamReader(wr.GetResponse().GetResponseStream()))
                {
                    return new Lyric(System.Net.WebUtility.HtmlDecode(
                        rp.ReadToEnd().Split(new[] { "<strLyric>", "</strLyric>" }, StringSplitOptions.None)[1])
                            .Split(new[] { "<br>" }, StringSplitOptions.RemoveEmptyEntries));
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
