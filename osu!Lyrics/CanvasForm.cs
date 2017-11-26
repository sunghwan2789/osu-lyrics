using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using osu_Lyrics.Properties;
using System.Threading;
using osu_Lyrics.Interop;
using osu_Lyrics.Forms;
using osu_Lyrics.Audio;
using osu_Lyrics.Lyrics;

namespace osu_Lyrics
{
    internal partial class CanvasForm : GhostLayeredForm
    {
        #region Lyrics()

        public static CanvasForm Constructor;
        public LyricManager lyricManager = new LyricManager();

        public CanvasForm()
        {
            if (Constructor == null)
            {
                Constructor = this;
            }
            InitializeComponent();

            //Osu.MessageReceived += Osu_MessageReceived;
            // Invoke these
            lyricManager.LyricChanged += (s, e) => Refresh();
            lyricManager.PlaySpeedChanged += (s, e) => Refresh();
            lyricManager.PlayTimeChanged += (s, e) => Refresh();
            lyricManager.AudioChanged += (s, e) => Refresh();
            Osu.KeyDown += Osu_KeyDown;
        }

        ~CanvasForm()
        {
            Osu.KeyDown -= Osu_KeyDown;
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
            Notice(Constants._MutexName);
        }

        private async void Lyrics_Shown(object sender, EventArgs e)
        {
            // 초기 설정을 위해 대화 상자 열기
            if (!File.Exists(Settings._Path))
            {
                BeginInvoke(new MethodInvoker(menuSetting.PerformClick));
            }
            while (!Osu.Process.HasExited)
            {
                if (Osu.IsForeground)
                {
                    if (!Location.Equals(Osu.ClientLocation))
                    {
                        Location = Osu.ClientLocation;
                    }
                    if (!Size.Equals(Osu.ClientSize))
                    {
                        Size = Osu.ClientSize;
                        Settings.DrawingOrigin = Point.Empty;
                    }
                    if (!(Settings?.Visible ?? false))
                    {
                        TopMost = true;
                    }
                    Visible = true;
                }
                else if (Settings?.Visible ?? false)
                {
                    Visible = true;
                }
                else if (Settings.ShowWhileOsuTop)
                {
                    Visible = false;
                }

                Refresh();

                await Task.Delay(Settings.RefreshRate);
            }
            Close();
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
            var lyric = lyricManager.GetLyricAtNow();
            foreach (var l in lyricManager.TruncateLyric(lyric))
            {
                lyricBuilder.AppendLine(l.Text);
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





        private void Osu_KeyDown(object sender, KeyEventArgs e)
        {
            // 설정 중이면 키보드 후킹 안 하기!
            if (Settings?.Visible ?? false)
            {
                return;
            }

            // 매칭되는 핫키가 있다면 osu!로 키 전송 방지
            e.SuppressKeyPress = Settings.SuppressKey;

            if (e.KeyData == Settings.KeyToggle)
            {
                showLyric = !showLyric;
                Notice("가사 {0}", showLyric ? "보임" : "숨김");
                return;
            }

            // 가사 보임 상태에서만 처리하는 핫키들
            if (!Settings.BlockSyncOnHide || (Settings.BlockSyncOnHide && showLyric))
            {
                if (e.KeyData == Settings.KeyBackward)
                {
                    lyricManager.LyricSync += 0.5;
                    Notice("싱크 느리게({0}초)", lyricManager.LyricSync.ToString("F1"));
                    return;
                }
                if (e.KeyData == Settings.KeyForward)
                {
                    lyricManager.LyricSync -= 0.5;
                    Notice("싱크 빠르게({0}초)", lyricManager.LyricSync.ToString("F1"));
                    return;
                }
            }

            // 매칭되는 핫키가 없으므로 osu!로 키 전송
            e.SuppressKeyPress = false;
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
                Settings.Dispose();
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