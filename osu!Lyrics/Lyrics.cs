using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using osu_Lyrics.Properties;

namespace osu_Lyrics
{
    internal partial class Lyrics : Form
    {
        #region Lyrics()

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("user32.dll")]
        private static extern bool UpdateLayeredWindow(IntPtr hwnd,
            IntPtr hdcDst,
            ref Point pptDst,
            ref Size psize,
            IntPtr hdcSrc,
            ref Point pprSrc,
            int crKey,
            ref BLENDFUNCTION pblend,
            int dwFlags);

        private struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

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
                const int WS_EX_LAYERED = 0x80000;
                const int WS_EX_TRANSPARENT = 0x20;

                var cp = base.CreateParams;
                cp.ExStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT;
                return cp;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            const byte AC_SRC_OVER = 0;
            const byte AC_SRC_ALPHA = 1;
            const int ULW_ALPHA = 2;

            var hDC = GetDC(IntPtr.Zero);
            var hMemDC = CreateCompatibleDC(hDC);
            var hBitmap = IntPtr.Zero;
            var hOldBitmap = IntPtr.Zero;

            Bitmap bmp = null;
            Graphics g = null;
            try
            {
                bmp = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
                g = Graphics.FromImage(bmp);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                DrawLyric(g);
                hBitmap = bmp.GetHbitmap(Color.FromArgb(0));
                hOldBitmap = SelectObject(hMemDC, hBitmap);

                var cur = Location;
                var size = bmp.Size;
                var point = Point.Empty;
                var blend = new BLENDFUNCTION
                {
                    BlendOp = AC_SRC_OVER,
                    BlendFlags = 0,
                    SourceConstantAlpha = 255,
                    AlphaFormat = AC_SRC_ALPHA
                };
                UpdateLayeredWindow(Handle, hDC, ref cur, ref size, hMemDC, ref point, 0, ref blend, ULW_ALPHA);
            }
            catch {}
            if (g != null)
            {
                g.Dispose();
            }
            if (bmp != null)
            {
                bmp.Dispose();
            }

            ReleaseDC(IntPtr.Zero, hDC);
            if (hBitmap != IntPtr.Zero)
            {
                SelectObject(hMemDC, hOldBitmap);
                DeleteObject(hBitmap);
            }
            DeleteDC(hMemDC);
        }

        #endregion

        private void Lyrics_Load(object sender, EventArgs e)
        {
            Osu.Show();
            Notice("osu!Lyrics {0}", Osu.Listen(Osu_Signal) ? Application.ProductVersion : "초기화 실패");

            Osu.HookKeyboard(Osu_KeyDown);
            backgroundWorker1.RunWorkerAsync();

            if (!File.Exists(Settings._Path))
            {
                Task.Factory.StartNew(() => Invoke(new MethodInvoker(toolStripMenuItem1.PerformClick)));
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!Osu.Process.HasExited)
            {
                Osu.Refresh();
                if (!Osu.Show(true))
                {
                    TopMost = Visible = true;
                    if (!Location.Equals(Osu.Location))
                    {
                        Location = Osu.Location;
                    }
                    if (!ClientSize.Equals(Osu.ClientSize))
                    {
                        ClientSize = Osu.ClientSize;
                        Settings.DrawingOrigin = Point.Empty;
                    }
                }
                else
                {
                    Visible = false;
                }

                if (NewLyricAvailable())
                {
                    Refresh();
                }

                Thread.Sleep(Settings.RefreshRate);
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
            Refresh();

            timer1.Start();
        }

        private void Notice(string format, params object[] args)
        {
            Notice(string.Format(format, args));
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            _notice = null;
            Invalidate();
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
        private static List<Lyric> GetLyrics(IDictionary<string, string> data)
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
            wr.ContentType = "application/soap+xml; charset=utf-8";
            wr.Headers.Add("SOAPAction", "ALSongWebServer/" + act);

            using (var rq = new StreamWriter(wr.GetRequestStream()))
            {
                rq.Write(content);
            }

            using (var rp = new StreamReader(wr.GetResponse().GetResponseStream()))
            {
                return
                    WebUtility.HtmlDecode(
                        rp.ReadToEnd().Split(new[] { "<strLyric>", "</strLyric>" }, StringSplitOptions.None)[1])
                        .Split(new[] { "<br>" }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(i => new Lyric(i))
                        .Where(i => i.Text.Length != 0)
                        .ToList();

                #region FOR DEBUG

                /*var builder = new StringBuilder();
                var d2 = d.Remove(0, 38).Split(new[] { "<" }, StringSplitOptions.RemoveEmptyEntries);
                var level = 0;
                
                foreach (var line in d2)
                {
                    if (line.StartsWith("/"))
                    {
                        level--;
                        continue;
                    }

                    for (var i = 0; i < level; i++)
                    {
                        builder.Append("  ");
                    }
                    builder.Append("<");
                    builder.AppendLine(line);

                    if (!line.StartsWith("/") && !line.EndsWith("/>"))
                    {
                        level++;
                    }
                }

                using (var w = new StreamWriter(@"E:\a.txt", false, Encoding.UTF8))
                {
                    w.Write(builder.ToString());
                }
                Process.Start(@"E:\a.txt");*/

                #endregion
            }
        }






        private Audio curAudio = new Audio();

        private double _curTimeChanged;
        private double _curTime;

        private double curTime
        {
            get { return _curTime + Now() - _curTimeChanged - curAudio.Sync; }
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

        private void Osu_Signal(string[] data)
        {
            // [ time, audioPath, audioPosition, beatmapPath ]
            if (data[1] != curAudio.Path)
            {
                curAudio = new Audio(data[1]) { Beatmap = Osu.Directory + data[3] };
                UpdateLyrics(File.ReadAllText(curAudio.Beatmap));
            }
            curTime = DateTimeOffset.Now.Subtract(DateTimeOffset.FromFileTime(Convert.ToInt64(data[0], 16))).TotalSeconds +
                      curAudio.Info.Time(Convert.ToUInt32(data[2], 16));
        }



        private readonly Queue<Lyric> lyrics = new Queue<Lyric>();
        private List<Lyric> _lyricsCache = new List<Lyric> { new Lyric() };

        private List<Lyric> lyricsCache
        {
            get { return _lyricsCache; }
            set
            {
                _lyricsCache = value;
                lyrics.Clear();
                value.ForEach(lyrics.Enqueue);
                curLyric = new Lyric();
            }
        }

        private CancellationTokenSource _cts;

        private void UpdateLyrics(string beatmap = "")
        {
            if (_cts != null)
            {
                _cts.Cancel();
                return;
            }

            lyricsCache = new List<Lyric> { new Lyric(0, "가사 받는 중...") };
            _cts = new CancellationTokenSource();
            Task.Factory.StartNew(
                () =>
                {
                    List<Lyric> data;
                    try
                    {
                        var hash = "";
                        Invoke(new MethodInvoker(() => hash = curAudio.Info.Hash));
                        _cts.Token.ThrowIfCancellationRequested();
                        data = GetLyrics(new Dictionary<string, string> { { "[HASH]", hash } });
                        data.Insert(0, new Lyric());
                    }
                    catch
                    {
                        try
                        {
                            _cts.Token.ThrowIfCancellationRequested();
                            data =
                                GetLyrics(
                                    new Dictionary<string, string>
                                    {
                                        {
                                            "[TITLE]",
                                            Osu.GetBeatmapSetting(
                                                beatmap, "TitleUnicode", Osu.GetBeatmapSetting(beatmap, "Title"))
                                        },
                                        {
                                            "[ARTIST]",
                                            Osu.GetBeatmapSetting(
                                                beatmap, "ArtistUnicode", Osu.GetBeatmapSetting(beatmap, "Artist"))
                                        }
                                    });
                            data.Insert(0, new Lyric());
                        }
                        catch
                        {
                            data = new List<Lyric> { new Lyric(0, "가사 없음") };
                        }
                    }
                    _cts.Token.ThrowIfCancellationRequested();
                    Invoke(new MethodInvoker(() => { lyricsCache = data; }));
                }, _cts.Token).ContinueWith(
                    result =>
                    {
                        Invoke(
                            new MethodInvoker(
                                () =>
                                {
                                    _cts = null;
                                    if (result.IsCanceled)
                                    {
                                        UpdateLyrics(File.ReadAllText(curAudio.Beatmap));
                                    }
                                }));
                    });
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

        private readonly List<string> lyricBuffer = new List<string>();

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








        private void notifyIcon1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Osu.Show();
            }
        }

        public static Settings Settings;

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (Settings == null)
            {
                using (Settings = new Settings { TopMost = true })
                {
                    Settings.ShowDialog();
                }
                Settings = null;
            }
            else
            {
                Settings.TopMost = true;
                Settings.Focus();
            }
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}