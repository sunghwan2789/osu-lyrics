using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using osu_Lyrics.Audio;
using System.IO;

namespace osu_Lyrics.Lyrics.Sources
{
    abstract class AlsongSource : LyricSource
    {
        protected async Task<Lyric> GetLyricAsync(string action, string data)
        {
            var wr = Request.Create(@"http://lyrics.alsong.co.kr/alsongwebservice/service1.asmx");
            wr.Method = "POST";
            wr.UserAgent = "gSOAP";
            wr.ContentType = "application/soap+xml; charset=UTF-8";
            wr.Headers.Add("SOAPAction", $@"""ALSongWebServer/{action}""");

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
    }
}
