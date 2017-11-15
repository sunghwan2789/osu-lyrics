using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using osu_Lyrics.Properties;
using System.Threading;
using static osu_Lyrics.Interop.NativeMethods;

namespace osu_Lyrics
{
    [System.ComponentModel.DesignerCategory("code")]
    internal partial class Lyrics : LayeredWindow
    {
        #region Lyrics()

        public static Lyrics Constructor;

        public Lyrics()
        {
            if (Constructor == null)
            {
                Constructor = this;
            }
            InitializeComponent();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= WS_EX_NOACTIVATE;
                return cp;
            }
        }


        public override void Render(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            DrawLyric(g);
        }

        #endregion

        private void Lyrics_Load(object sender, EventArgs e)
        {
            Notice(Osu.Listen(Osu_Signal) ? Constants._MutexName : "초기화 실패");
            Osu.HookKeyboard(Osu_KeyDown);
        }

        private async void Lyrics_Shown(object sender, EventArgs e)
        {
            // 초기 설정을 위해 대화 상자 열기
            if (!File.Exists(Settings._Path))
            {
                Task.Run(() => Invoke(new MethodInvoker(menuSetting.PerformClick)));
            }
            while (!Osu.Process.HasExited)
            {
                if (Osu.IsForeground())
                {
                    if (!Location.Equals(Osu.Location))
                    {
                        Location = Osu.Location;
                    }
                    if (!Size.Equals(Osu.ClientSize))
                    {
                        Size = Osu.ClientSize;
                        Settings.DrawingOrigin = Point.Empty;
                    }
                    if (Settings == null)
                    {
                        TopMost = true;
                    }
                    Visible = true;
                }
                else if (Settings.ShowWhileOsuTop)
                {
                    Visible = false;
                }

                if (NewLyricAvailable())
                {
                    Refresh();
                }

                await Task.Delay(Settings.RefreshRate);
            }
            Close();
        }

        private void Lyrics_FormClosing(object sender, FormClosingEventArgs e)
        {
            Osu.UnhookKeyboard();
        }






        #region Notice(...)

        private string _notice;

        private void Notice(string value)
        {
            timer1.Stop();

            _notice = value;
            Invoke(new MethodInvoker(Refresh));

            timer1.Start();
        }

        private void Notice(string format, params object[] args)
        {
            Notice(string.Format(format, args));
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            _notice = null;
            Invoke(new MethodInvoker(Invalidate));
        }

        #endregion

        private static double Now()
        {
            return new TimeSpan(DateTime.Now.Ticks).TotalSeconds;
        }

        /// <summary>
        /// 알송 서버에서 가사를 가져옴.
        /// </summary>
        /// <param name="data">[HASH]: ... | [ARTIST]: ..., [TITLE]: ...</param>
        /// <returns>List&lt;string&gt;</returns>
        private static async Task<List<Lyric>> GetLyricsAsync(IDictionary<string, string> data)
        {
            try
            {
                var act = "GetLyric5";
                if (!data.ContainsKey("[HASH]"))
                {
                    act = "GetResembleLyric2";
                }
                var content = data.Aggregate(Resources.ResourceManager.GetString(act), (o, i) => o.Replace(i.Key, i.Value));

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
                    return WebUtility.HtmlDecode(rp.ReadToEnd().Split(new[] { "<strLyric>", "</strLyric>" }, StringSplitOptions.None)[1])
                        .Split(new[] { "<br>" }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(i => new Lyric(i))
                        .Where(i => i.Text.Length != 0)
                        .ToList();
                }
            }
            catch
            {
                return null;
            }
        }






        private Audio curAudio = new Audio();

        private double _curTimeChanged;
        private double _curTime;
        private double _playbackRate;

        private double curTime
        {
            get
            {
                var elapsedTime = (Now() - _curTimeChanged) *_playbackRate;
                return _curTime + elapsedTime - curAudio.Sync;
            }
            set
            {
                if (value < _curTime)
                {
                    lyricsCache = lyricsCache;
                }
                _curTimeChanged = Now();
                _curTime = value;
            }
        }
        
        private readonly Queue<Lyric> lyrics = new Queue<Lyric>();
        private List<Lyric> _lyricsCache = new List<Lyric> { new Lyric() };

        private List<Lyric> lyricsCache
        {
            get
            {
                return _lyricsCache;
            }
            set
            {
                _lyricsCache = value;
                lyrics.Clear();
                value.ForEach(lyrics.Enqueue);
                curLyric = new Lyric();
            }
        }

        private void Osu_Signal(string line)
        {
            var data = line.Split('|');
            if (data.Length != 5)
            {
                return;
            }
            // [ time, audioPath, audioCurrentTime, audioPlaybackRate, beatmapPath ]
            // 재생 중인 곡이 바꼈다!
            if (data[1] != curAudio.Path)
            {
                curAudio = new Audio(data[1], data[4]);
                UpdateLyrics();
            }
            curTime = DateTimeOffset.Now.Subtract(
                DateTimeOffset.FromFileTime(Convert.ToInt64(data[0], 16))
            ).TotalSeconds + Convert.ToDouble(data[2]);
            _playbackRate = 1 + Convert.ToDouble(data[3]) / 100;
        }

        private CancellationTokenSource cts;

        private void UpdateLyrics()
        {
            if (cts != null)
            {
                cts.Cancel();
                return;
            }

            lyricsCache = new List<Lyric>
            {
                new Lyric(0, "가사 받는 중...")
            };
            cts = new CancellationTokenSource();
            Task.Run(async () =>
            {
                cts.Token.ThrowIfCancellationRequested();
                // 파일 해시로 가사 검색
                var newLyrics = await GetLyricsAsync(new Dictionary<string, string>
                {
                    { "[HASH]", curAudio.Hash }
                });

                if (newLyrics == null && curAudio.Beatmap != null)
                {
                    cts.Token.ThrowIfCancellationRequested();
                    // 음악 정보로 가사 검색
                    newLyrics = await GetLyricsAsync(new Dictionary<string, string>
                    {
                        { "[TITLE]", curAudio.Beatmap.TitleUnicode ?? curAudio.Beatmap.Title },
                        { "[ARTIST]", curAudio.Beatmap.ArtistUnicode ?? curAudio.Beatmap.Artist }
                    });
                }

                if (newLyrics != null)
                {
                    newLyrics.Insert(0, new Lyric());
                }
                else
                {
                    newLyrics = new List<Lyric>
                    {
                        new Lyric(0, "가사 없음")
                    };
                }

                cts.Token.ThrowIfCancellationRequested();
                Invoke(new MethodInvoker(() =>
                {
                    lyricsCache = newLyrics;
                }));
            }, cts.Token).ContinueWith(result => Invoke(new MethodInvoker(() =>
            {
                cts = null;
                if (result.IsCanceled)
                {
                    UpdateLyrics();
                }
            })));
        }




        private Lyric _curLyric = new Lyric();

        private Lyric curLyric
        {
            get { return _curLyric; }
            set
            {
                _curLyric = value;
                lyricBuffer.Clear();
            }
        }

        private readonly List<string> lyricBuffer = new List<string>
        {
            "선곡하세요"
        };

        private bool NewLyricAvailable()
        {
            var flag = false;
            while (lyrics.Count > 0)
            {
                var lyric = lyrics.Peek();

                if (lyric.Time < curLyric.Time)
                {
                    lyrics.Dequeue();
                    continue;
                }

                if (lyric.Time <= curTime)
                {
                    if (!lyric.Time.Equals(curLyric.Time) || (lyric.Time.Equals(0) && curLyric.Time.Equals(0)))
                    {
                        curLyric = lyric;
                        flag = true;
                    }
                    lyricBuffer.Add(lyric.Text);
                    lyrics.Dequeue();
                }
                else
                {
                    break;
                }
            }
            return flag;
        }





        private bool showLyric = true;

        private void DrawLyric(Graphics g)
        {
            if (_notice != null)
            {
                using (var path = new GraphicsPath())
                {
                    path.AddString(
                        _notice, Settings.FontFamily, Settings.FontStyle, g.DpiY * 14 / 72, Point.Empty,
                        StringFormat.GenericDefault);
                    if (Settings.BorderWidth > 0)
                    {
                        g.DrawPath(Settings.Border, path);
                    }
                    g.FillPath(Settings.Brush, path);
                }
            }

            if (!showLyric)
            {
                return;
            }

            var lyricBuilder = new StringBuilder();
            var lyricCount = lyricBuffer.Count;
            if (Settings.LineCount == 0)
            {
                foreach (var i in lyricBuffer)
                {
                    lyricBuilder.AppendLine(i);
                }
            }
            else if (Settings.LineCount > 0)
            {
                for (var i = 0; i < Settings.LineCount && i < lyricCount; i++)
                {
                    lyricBuilder.AppendLine(lyricBuffer[i]);
                }
            }
            else
            {
                var i = lyricCount + Settings.LineCount;
                if (i < 0)
                {
                    i = 0;
                }
                for (; i < lyricCount; i++)
                {
                    lyricBuilder.AppendLine(lyricBuffer[i]);
                }
            }

            using (var path = new GraphicsPath())
            {
                path.AddString(
                    lyricBuilder.ToString(), Settings.FontFamily, Settings.FontStyle, g.DpiY * Settings.FontSize / 72,
                    Settings.DrawingOrigin, Settings.StringFormat);
                if (Settings.BorderWidth > 0)
                {
                    g.DrawPath(Settings.Border, path);
                }
                g.FillPath(Settings.Brush, path);
            }
        }





        private bool Osu_KeyDown(Keys key)
        {
            if (key == Settings.KeyToggle)
            {
                showLyric = !showLyric;
                Notice("가사 {0}", showLyric ? "보임" : "숨김");
                return true;
            }
            if (!Settings.BlockSyncOnHide || (Settings.BlockSyncOnHide && showLyric))
            {
                if (key == Settings.KeyBackward)
                {
                    curAudio.Sync += 0.5;
                    lyricsCache = lyricsCache;
                    Notice("싱크 느리게({0}초)", curAudio.Sync.ToString("F1"));
                    return true;
                }
                if (key == Settings.KeyForward)
                {
                    curAudio.Sync -= 0.5;
                    Notice("싱크 빠르게({0}초)", curAudio.Sync.ToString("F1"));
                    return true;
                }
            }
            return false;
        }








        private void trayIcon_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Osu.Show();
            }
        }

        public static Settings Settings;

        private void menuSetting_Click(object sender, EventArgs e)
        {
            if (Settings == null)
            {
                Settings = new Settings
                {
                    TopMost = true
                };
                Settings.ShowDialog();
                Settings = null;
            }
            else
            {
                Settings.TopMost = true;
                Settings.Focus();
            }
        }

        private void menuExit_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}