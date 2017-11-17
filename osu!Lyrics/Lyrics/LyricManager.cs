using osu_Lyrics.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace osu_Lyrics.Lyrics
{
    class LyricManager
    {
        private LyricLine currentLine = new LyricLine();
        private Lyric lyric = new Lyric();
        public Lyric Lyric
        {
            get { return lyric; }
            set
            {
                lyric = value;
                LyricSync = 0;
                LyricChanged?.Invoke(null, null);
            }
        }
        private string status = "선곡하세요";

        private static double Now => new TimeSpan(DateTime.Now.Ticks).TotalSeconds;

        /// <summary>
        /// TODO when lyric reset, sync reset
        /// </summary>
        private double lyricSync = 0;
        public double LyricSync
        {
            get { return lyricSync; }
            set
            {
                lyricSync = value;
                currentLine = Lyric.FirstOrDefault() ?? new LyricLine();
            }
        }

        public double PlayTimeElapsed
        {
            get
            {
                var elapsedTime = (Now - playTimeChanged) * PlaySpeed;
                return PlayTime + elapsedTime - LyricSync;
            }
        }

        private double playTimeChanged = 0;
        private double playTime = 0;
        public double PlayTime
        {
            get { return playTime; }
            set
            {
                playTime = value;
                playTimeChanged = Now;
                currentLine = Lyric.FirstOrDefault() ?? new LyricLine();
                PlayTimeChanged?.Invoke(null, null);
            }
        }

        private double playSpeed = 0;
        public double PlaySpeed
        {
            get { return playSpeed; }
            set
            {
                playSpeed = value;
                PlaySpeedChanged?.Invoke(null, null);
            }
        }

        public string AudioPath { get; set; }
        public Audio.AudioInfo AudioInfo { get; set; }
        public Beatmap.BeatmapMetadata BeatmapMetadata { get; set; }

        public LyricManager()
        {
            Osu.MessageReceived += Osu_MessageReceived;
        }

        ~LyricManager()
        {
            Osu.MessageReceived -= Osu_MessageReceived;
        }

        public event EventHandler AudioChanged;
        public event EventHandler LyricChanged;
        public event EventHandler PlayTimeChanged;
        public event EventHandler PlaySpeedChanged;

        private void Osu_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            // 재생 중인 곡이 바꼈다!
            if (e.AudioPath != AudioPath)
            {
                AudioPath = e.AudioPath;
                using (var fs = new FileStream(e.AudioPath, FileMode.Open, FileAccess.Read))
                {
                    AudioInfo = Audio.Formats.AudioDecoder.GetDecoder(fs)?.Decode(fs);
                }
                using (var sr = new StreamReader(e.BeatmapPath))
                {
                    BeatmapMetadata = Beatmap.Formats.BeatmapDecoder.GetDecoder(sr)?.Decode(sr);
                }

                FetchLyric();
            }
            PlayTime = DateTime.Now.Subtract(e.CreatedTime).TotalSeconds + e.AudioPlayTime;
            PlaySpeed = 1 + e.AudioPlaySpeed / 100;
        }

        private CancellationTokenSource cts;
        private void FetchLyric()
        {
            if (cts != null)
            {
                cts.Cancel();
                return;
            }

            cts = new CancellationTokenSource();
            status = "가사 받는 중...";
            Lyric.Clear();
            AudioChanged?.BeginInvoke(null, null, null, null);
            Task.Run(async () =>
            {
                cts.Token.ThrowIfCancellationRequested();
                // 파일 해시로 가사 검색
                var newLyrics = await GetLyricsAsync(new Dictionary<string, string>
                {
                    { "[HASH]", AudioInfo.CheckSum }
                });

                if (newLyrics == null && BeatmapMetadata != null)
                {
                    cts.Token.ThrowIfCancellationRequested();
                    // 음악 정보로 가사 검색
                    newLyrics = await GetLyricsAsync(new Dictionary<string, string>
                    {
                        { "[TITLE]", BeatmapMetadata.TitleUnicode ?? BeatmapMetadata.Title },
                        { "[ARTIST]", BeatmapMetadata.ArtistUnicode ?? BeatmapMetadata.Artist }
                    });
                }

                newLyrics?.Insert(0, new LyricLine());

                cts.Token.ThrowIfCancellationRequested();
                if (newLyrics == null)
                {
                    status = "가사 없음";
                    Lyric.Clear();
                }
                else
                {
                    Lyric = newLyrics;
                }
            }, cts.Token).ContinueWith(result =>
            {
                cts = null;
                if (result.IsCanceled)
                {
                    FetchLyric();
                }
            });
        }

        public Lyric GetLyric()
        {
            if (!lyric.Any())
            {
                return new Lyric
                {
                    new LyricLine(0, status)
                };
            }

            var ret = new Lyric();
            var end = Lyric.SkipWhile(i => i.Time < PlayTimeElapsed).FirstOrDefault();
            foreach (var line in Lyric.SkipWhile(i => i.Time < currentLine.Time))
            {
                if (end != null && line.Time >= end.Time)
                {
                    break;
                }
                if (line.Time > currentLine.Time)
                {
                    currentLine = line;
                    ret.Clear();
                }
                ret.Add(line);
            }
            return ret;
        }

        /// <summary>
        /// 알송 서버에서 가사를 가져옴.
        /// </summary>
        /// <param name="data">[HASH]: ... | [ARTIST]: ..., [TITLE]: ...</param>
        /// <returns>List&lt;string&gt;</returns>
        private static async Task<Lyric> GetLyricsAsync(IDictionary<string, string> data)
        {
            try
            {
                var act = "GetLyric5";
                if (!data.ContainsKey("[HASH]"))
                {
                    act = "GetResembleLyric2";
                }
                var content = data.Aggregate(Properties.Resources.ResourceManager.GetString(act), (o, i) => o.Replace(i.Key, i.Value));

                var wr = Request.Create(@"http://lyrics.alsong.co.kr/alsongwebservice/service1.asmx");
                wr.Method = "POST";
                wr.UserAgent = "gSOAP";
                wr.ContentType = "application/soap+xml; charset=UTF-8";
                wr.Headers.Add("SOAPAction", "ALSongWebServer/" + act);

                using (var rq = new StreamWriter(wr.GetRequestStream()))
                {
                    rq.Write(content);
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
