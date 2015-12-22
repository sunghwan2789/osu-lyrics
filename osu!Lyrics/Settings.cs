using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace osu_Lyrics
{
    internal partial class Settings : Form
    {
        public Settings()
        {
            InitializeComponent();
        }

        private bool Loaded;

        [DllImport("kernel32.dll")]
        private static extern uint GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, uint nSize, string lpFileName);

        [DllImport("kernel32.dll")]
        private static extern bool WritePrivateProfileString(string lpAppName, string lpKeyName, string lpString, string lpFileName);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(Keys vKey);

        public static readonly string _MutexName = "osu!Lyrics." + Application.ProductVersion;
        public static readonly string _Path = Application.ExecutablePath + ".cfg";
        public static readonly string _Server = Path.Combine(Path.GetTempPath(), "osu!Lyrics.dll");
        public static readonly string _BakExt = ".del";

        private static string Get(string section, string key)
        {
            var temp = new StringBuilder(255);
            GetPrivateProfileString(section, key, "", temp, (uint) temp.Capacity, _Path);
            return temp.ToString();
        }

        private static void Set(string section, string key, string value)
        {
            WritePrivateProfileString(section, key, value, _Path);
        }

        private static int _LineCount = 0x40;
        private static int _Opacity = -1;
        private static int _VerticalAlign = -1;
        private static int _VerticalOffset = -1;
        private static int _HorizontalAlign = -1;
        private static int _HorizontalOffset = -1;
        private static FontFamily _FontFamily = null;
        private static int _FontStyle = -1;
        private static int _FontSize = -1;
        private static int _FontColor = -1;
        private static int _BorderWidth = -1;
        private static int _BorderColor = -1;
        private static int _RefreshRate = -1;
        private static int _KeyBackward = -1;
        private static int _KeyForward = -1;
        private static int _KeyToggle = -1;
        private static int _BlockSyncOnHide = -1;
        private static int _SuppressKey = -1;
        private static int _ShowWhileOsuTop = -1;

        public static int LineCount
        {
            get
            {
                if (_LineCount == 0x40)
                {
                    try
                    {
                        _LineCount = Convert.ToInt32(Get("LAYOUT", "LineCount"));
                    }
                    catch
                    {
                        _LineCount = 0;
                    }
                }
                return (_LineCount > 0 ? 1 : -1) * (Math.Abs(_LineCount) & 0x3F);
            }
        }

        public new static int Opacity
        {
            get
            {
                if (_Opacity < 0 || _Opacity > 100)
                {
                    try
                    {
                        _Opacity = Convert.ToInt32(Get("DESIGN", "Opacity"));
                        if (_Opacity < 0 || _Opacity > 100)
                        {
                            throw new OverflowException();
                        }
                    }
                    catch
                    {
                        _Opacity = 100;
                    }
                }
                return _Opacity;
            }
        }

        public static StringAlignment VerticalAlign
        {
            get
            {
                if (_VerticalAlign < 0)
                {
                    try
                    {
                        _VerticalAlign = Convert.ToInt32(Get("LAYOUT", "VerticalAlign"));
                        if (_VerticalAlign < 0)
                        {
                            throw new OverflowException();
                        }
                    }
                    catch
                    {
                        _VerticalAlign = 0;
                    }
                }
                return (StringAlignment) _VerticalAlign;
            }
        }

        public static int VerticalOffset
        {
            get
            {
                if (_VerticalOffset < 0)
                {
                    try
                    {
                        _VerticalOffset = Convert.ToInt32(Get("LAYOUT", "VerticalOffset"));
                        if (_VerticalOffset < 0)
                        {
                            throw new OverflowException();
                        }
                    }
                    catch
                    {
                        _VerticalOffset = 50;
                    }
                }
                return (_VerticalAlign > 0 ? _VerticalAlign == 1 ? 0 : -1 : 1) * _VerticalOffset;
            }
        }

        public static StringAlignment HorizontalAlign
        {
            get
            {
                if (_HorizontalAlign < 0)
                {
                    try
                    {
                        _HorizontalAlign = Convert.ToInt32(Get("LAYOUT", "HorizontalAlign"));
                        if (_HorizontalAlign < 0)
                        {
                            throw new OverflowException();
                        }
                    }
                    catch
                    {
                        _HorizontalAlign = 1;
                    }
                }
                return (StringAlignment) _HorizontalAlign;
            }
        }

        public static int HorizontalOffset
        {
            get
            {
                if (_HorizontalOffset < 0)
                {
                    try
                    {
                        _HorizontalOffset = Convert.ToInt32(Get("LAYOUT", "HorizontalOffset"));
                        if (_HorizontalOffset < 0)
                        {
                            throw new OverflowException();
                        }
                    }
                    catch
                    {
                        _HorizontalOffset = 0;
                    }
                }
                return (_HorizontalAlign > 0 ? _HorizontalAlign == 1 ? 0 : -1 : 1) * _HorizontalOffset;
            }
        }

        public static FontFamily FontFamily
        {
            get
            {
                if (_FontFamily == null)
                {
                    try
                    {
                        _FontFamily = new FontFamily(Get("DESIGN", "FontFamily"));
                    }
                    catch
                    {
                        _FontFamily = FontFamily.GenericSerif;
                    }
                }
                return _FontFamily;
            }
        }

        public static int FontStyle
        {
            get
            {
                if (_FontStyle < 0)
                {
                    try
                    {
                        _FontStyle = Convert.ToInt32(Get("DESIGN", "FontStyle"));
                        if (Enum.ToObject(typeof(FontStyle), _FontStyle) == null)
                        {
                            throw new OverflowException();
                        }
                    }
                    catch
                    {
                        _FontStyle = (int) System.Drawing.FontStyle.Regular;
                    }
                }
                return _FontStyle;
            }
        }

        public static int FontSize
        {
            get
            {
                if (_FontSize < 0)
                {
                    try
                    {
                        _FontSize = Convert.ToInt32(Get("DESIGN", "FontSize"));
                        if (_FontSize < 0)
                        {
                            throw new OverflowException();
                        }
                    }
                    catch
                    {
                        _FontSize = 24;
                    }
                }
                return _FontSize;
            }
        }

        public static Color FontColor
        {
            get
            {
                if (_FontColor < 0)
                {
                    try
                    {
                        _FontColor = Convert.ToInt32(Get("DESIGN", "FontColor"), 16);
                        if (_FontColor < 0)
                        {
                            throw new OverflowException();
                        }
                    }
                    catch
                    {
                        _FontColor = 0xFFFFFF;
                    }
                }
                return Color.FromArgb(Opacity * 255 / 100 << 24 | _FontColor);
            }
        }

        public static int BorderWidth
        {
            get
            {
                if (_BorderWidth < 0)
                {
                    try
                    {
                        _BorderWidth = Convert.ToInt32(Get("DESIGN", "BorderWidth"));
                        if (_BorderWidth < 0)
                        {
                            throw new OverflowException();
                        }
                    }
                    catch
                    {
                        _BorderWidth = 2;
                    }
                }
                return _BorderWidth;
            }
        }

        public static Color BorderColor
        {
            get
            {
                if (_BorderColor < 0)
                {
                    try
                    {
                        _BorderColor = Convert.ToInt32("0x" + Get("DESIGN", "BorderColor"), 16);
                        if (_BorderColor < 0)
                        {
                            throw new OverflowException();
                        }
                    }
                    catch
                    {
                        _BorderColor = 0x000000;
                    }
                }
                return Color.FromArgb(Opacity * 255 / 100 << 24 | _BorderColor);
            }
        }

        public static int RefreshRate
        {
            get
            {
                if (_RefreshRate < 0)
                {
                    try
                    {
                        _RefreshRate = Convert.ToInt32(Get("PROGRAM", "RefreshRate"));
                        if (_RefreshRate < 0)
                        {
                            throw new OverflowException();
                        }
                    }
                    catch
                    {
                        _RefreshRate = 100;
                    }
                }
                return _RefreshRate;
            }
        }

        public static Keys KeyBackward
        {
            get
            {
                if (_KeyBackward < 0)
                {
                    try
                    {
                        _KeyBackward = Convert.ToInt32(Get("PROGRAM", "KeyBackward"));
                        if (_KeyBackward < 0)
                        {
                            throw new OverflowException();
                        }
                    }
                    catch
                    {
                        _KeyBackward = (int) Keys.None;
                    }
                }
                return (Keys) _KeyBackward;
            }
        }

        public static Keys KeyForward
        {
            get
            {
                if (_KeyForward < 0)
                {
                    try
                    {
                        _KeyForward = Convert.ToInt32(Get("PROGRAM", "KeyForward"));
                        if (_KeyForward < 0)
                        {
                            throw new OverflowException();
                        }
                    }
                    catch
                    {
                        _KeyForward = (int) Keys.None;
                    }
                }
                return (Keys) _KeyForward;
            }
        }

        public static Keys KeyToggle
        {
            get
            {
                if (_KeyToggle < 0)
                {
                    try
                    {
                        _KeyToggle = Convert.ToInt32(Get("PROGRAM", "KeyToggle"));
                        if (_KeyToggle < 0)
                        {
                            throw new OverflowException();
                        }
                    }
                    catch
                    {
                        _KeyToggle = (int) Keys.None;
                    }
                }
                return (Keys) _KeyToggle;
            }
        }

        public static bool BlockSyncOnHide
        {
            get
            {
                if (_BlockSyncOnHide < 0 || _BlockSyncOnHide > 1)
                {
                    try
                    {
                        _BlockSyncOnHide = Convert.ToInt32(Get("PROGRAM", "BlockSyncOnHide"));
                        if (_BlockSyncOnHide < 0 || _BlockSyncOnHide > 1)
                        {
                            throw new OverflowException();
                        }
                    }
                    catch
                    {
                        _BlockSyncOnHide = 0;
                    }
                }
                return _BlockSyncOnHide == 1;
            }
        }

        public static bool SuppressKey
        {
            get
            {
                if (_SuppressKey < 0 || _SuppressKey > 1)
                {
                    try
                    {
                        _SuppressKey = Convert.ToInt32(Get("PROGRAM", "SuppressKey"));
                        if (_SuppressKey < 0 || _SuppressKey > 1)
                        {
                            throw new OverflowException();
                        }
                    }
                    catch
                    {
                        _SuppressKey = 0;
                    }
                }
                return _SuppressKey == 1;
            }
        }

        public static bool ShowWhileOsuTop
        {
            get
            {
                if (_ShowWhileOsuTop < 0 || _ShowWhileOsuTop > 1)
                {
                    try
                    {
                        _ShowWhileOsuTop = Convert.ToInt32(Get("DESIGN", "ShowWhileOsuTop"));
                        if (_ShowWhileOsuTop < 0 || _ShowWhileOsuTop > 1)
                        {
                            throw new OverflowException();
                        }
                    }
                    catch
                    {
                        _ShowWhileOsuTop = 1;
                    }
                }
                return _ShowWhileOsuTop == 1;
            }
        }


        private static StringFormat _StringFormat = null;
        private static SolidBrush _Brush = null;
        private static Pen _Border = null;
        private static Point _DrawingOrigin = Point.Empty;

        public static StringFormat StringFormat
        {
            get
            {
                if (_StringFormat == null)
                {
                    _StringFormat = new StringFormat
                    {
                        Alignment = HorizontalAlign,
                        LineAlignment = VerticalAlign,
                        FormatFlags = StringFormatFlags.NoWrap,
                    };
                }
                return _StringFormat;
            }
        }

        public static SolidBrush Brush
        {
            get
            {
                if (_Brush == null)
                {
                    _Brush = new SolidBrush(FontColor);
                }
                return _Brush;
            }
        }

        public static Pen Border
        {
            get
            {
                if (_Border == null)
                {
                    _Border = new Pen(BorderColor, BorderWidth) { LineJoin = LineJoin.Round };
                }
                return _Border;
            }
        }

        public static Point DrawingOrigin
        {
            get
            {
                if (_DrawingOrigin.IsEmpty)
                {
                    _DrawingOrigin = new Point(
                        HorizontalOffset + (int) HorizontalAlign * Lyrics.Constructor.Width / 2,
                        VerticalOffset + (int) VerticalAlign * Lyrics.Constructor.Height / 2);
                }
                return _DrawingOrigin;
            }
            set { _DrawingOrigin = value; }
        }


        private Font __Font;
        private Color __FontColor;
        private Color __BorderColor;

        private void Settings_Load(object sender, EventArgs e)
        {
            label17.Text = Application.ProductVersion;
            numericUpDown1.Value = LineCount;
            trackBar1.Value = Opacity;
            comboBox1.SelectedIndex = (int) VerticalAlign;
            numericUpDown2.Value = Math.Abs(VerticalOffset);
            comboBox2.SelectedIndex = (int) HorizontalAlign;
            numericUpDown3.Value = Math.Abs(HorizontalOffset);
            __Font = new Font(FontFamily, FontSize, (FontStyle) FontStyle, GraphicsUnit.Point);
            __FontColor = FontColor;
            numericUpDown4.Value = BorderWidth;
            __BorderColor = BorderColor;
            numericUpDown5.Value = RefreshRate;
            textBox1.Text = KeyBackward.ToString();
            textBox1.Tag = KeyBackward;
            textBox2.Text = KeyForward.ToString();
            textBox2.Tag = KeyForward;
            textBox3.Text = KeyToggle.ToString();
            textBox3.Tag = KeyToggle;
            checkBox1.Checked = BlockSyncOnHide;
            checkBox2.Checked = SuppressKey;
            checkBox3.Checked = ShowWhileOsuTop;
            Loaded = true;
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            label3.Text = trackBar1.Value.ToString("#0'%'");
            UpdateSettings();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            numericUpDown2.Enabled = comboBox1.SelectedIndex != 1;
            UpdateSettings();
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            numericUpDown3.Enabled = comboBox2.SelectedIndex != 1;
            UpdateSettings();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            fontDialog1.Font = __Font;
            if (fontDialog1.ShowDialog() == DialogResult.OK)
            {
                __Font = fontDialog1.Font;
                UpdateSettings();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            colorDialog1.Color = __FontColor;
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                __FontColor = colorDialog1.Color;
                UpdateSettings();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            colorDialog1.Color = __BorderColor;
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                __BorderColor = colorDialog1.Color;
                UpdateSettings();
            }
        }

        private void UpdateSettings(object sender = null, EventArgs e = null)
        {
            if (!Loaded)
            {
                return;
            }

            _LineCount = (int) numericUpDown1.Value;
            _Opacity = trackBar1.Value;
            _VerticalAlign = comboBox1.SelectedIndex;
            _VerticalOffset = (int) numericUpDown2.Value;
            _HorizontalAlign = comboBox2.SelectedIndex;
            _HorizontalOffset = (int) numericUpDown3.Value;
            _FontFamily = __Font.FontFamily;
            _FontStyle = (int) __Font.Style;
            _FontSize = (int) Math.Round(__Font.Size);
            _FontColor = __FontColor.ToArgb() & 0xFFFFFF;
            _BorderWidth = (int) numericUpDown4.Value;
            _BorderColor = __BorderColor.ToArgb() & 0xFFFFFF;
            _RefreshRate = (int) numericUpDown5.Value;
            _KeyBackward = (int) textBox1.Tag;
            _KeyForward = (int) textBox2.Tag;
            _KeyToggle = (int) textBox3.Tag;
            _BlockSyncOnHide = checkBox1.Checked ? 1 : 0;
            _SuppressKey = checkBox2.Checked ? 1 : 0;
            _ShowWhileOsuTop = checkBox3.Checked ? 1 : 0;

            UpdateScreen();
        }

        private static void UpdateScreen()
        {
            _StringFormat = null;
            _Brush = null;
            _Border = null;
            _DrawingOrigin = Point.Empty;

            Lyrics.Constructor.Invoke(new MethodInvoker(() =>
            {
                Lyrics.Constructor.Visible = true;
                Lyrics.Constructor.Refresh();
            }));
        }

        private void Settings_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.OK)
            {
                UpdateSettings();

                Set("LAYOUT", "LineCount", _LineCount.ToString());
                Set("DESIGN", "Opacity", _Opacity.ToString());
                Set("LAYOUT", "VerticalAlign", _VerticalAlign.ToString());
                Set("LAYOUT", "VerticalOffset", _VerticalOffset.ToString());
                Set("LAYOUT", "HorizontalAlign", _HorizontalAlign.ToString());
                Set("LAYOUT", "HorizontalOffset", _HorizontalOffset.ToString());
                Set("DESIGN", "FontFamily", _FontFamily.Name);
                Set("DESIGN", "FontStyle", _FontStyle.ToString());
                Set("DESIGN", "FontSize", _FontSize.ToString());
                Set("DESIGN", "FontColor", _FontColor.ToString("X6"));
                Set("DESIGN", "BorderWidth", _BorderWidth.ToString());
                Set("DESIGN", "BorderColor", _BorderColor.ToString("X6"));
                Set("PROGRAM", "RefreshRate", _RefreshRate.ToString());
                Set("PROGRAM", "KeyBackward", _KeyBackward.ToString());
                Set("PROGRAM", "KeyForward", _KeyForward.ToString());
                Set("PROGRAM", "KeyToggle", _KeyToggle.ToString());
                Set("PROGRAM", "BlockSyncOnHide", _BlockSyncOnHide.ToString());
                Set("PROGRAM", "SuppressKey", _SuppressKey.ToString());
                Set("DESIGN", "ShowWhileOsuTop", _ShowWhileOsuTop.ToString());
            }
            else
            {
                _LineCount = 0x40;
                _Opacity = -1;
                _VerticalAlign = -1;
                _VerticalOffset = -1;
                _HorizontalAlign = -1;
                _HorizontalOffset = -1;
                _FontFamily = null;
                _FontStyle = -1;
                _FontSize = -1;
                _FontColor = -1;
                _BorderWidth = -1;
                _BorderColor = -1;
                _RefreshRate = -1;
                _KeyBackward = -1;
                _KeyForward = -1;
                _KeyToggle = -1;
                _BlockSyncOnHide = -1;
                _SuppressKey = -1;
                _ShowWhileOsuTop = -1;

                UpdateScreen();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }



        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;

            var key = e.KeyCode;
            if (key == Keys.Escape)
            {
                key = 0;
            }
            else
            {
                var flag = false;
                foreach (var i in
                    new Dictionary<Keys, Keys[]>
                    {
                        { Keys.ControlKey, new[] { Keys.LControlKey, Keys.RControlKey } },
                        { Keys.Menu, new[] { Keys.LMenu, Keys.RMenu } },
                        { Keys.ShiftKey, new[] { Keys.LShiftKey, Keys.RShiftKey } }
                    })
                {
                    if (i.Key == key)
                    {
                        foreach (var j in i.Value.Where(k => Convert.ToBoolean(GetAsyncKeyState(k))))
                        {
                            flag = true;
                            key = j;
                            break;
                        }
                    }
                    if (flag)
                    {
                        break;
                    }
                }
            }

            var textBox = (TextBox) sender;
            textBox.Text = key.ToString();
            textBox.Tag = key;
            UpdateSettings();
        }



        private void button6_Click(object sender, EventArgs e)
        {
            const string url = "http://bloodcat.com/_data/static/lv.txt";

            try
            {
                var current = Version.Parse(Application.ProductVersion);
                Version latest = null;
                var restartOsu = false;
                var changes = new List<string>();
                using (var sr = new StreamReader(Request.Create(url).GetResponse().GetResponseStream(), Encoding.UTF8))
                {
                    while (!sr.EndOfStream)
                    {
                        var data = sr.ReadLine().Split('|');
                        var version = Version.Parse(data[0]);
                        if (latest == null)
                        {
                            latest = version;
                        }
                        if (version <= current)
                        {
                            break;
                        }
                        if (data[1][0] == '_')
                        {
                            restartOsu = true;
                            data[1] = data[1].Remove(0, 1);
                        }
                        changes.Add(data[1].Replace(';', '\n'));
                    }
                }
                if (changes.Any())
                {
                    changes.Reverse();
                    var sb = new StringBuilder();
                    sb.AppendFormat("최신 버전으로 업데이트할까요?{0}\n\n", restartOsu ? " (주의! osu! 재시작됨!!)" : "");
                    sb.AppendFormat("{0}->{1} 변경사항\n", current, latest);
                    changes.ForEach(i => sb.AppendLine(i));
                    if (MessageBox.Show(sb.ToString(), "업데이트 발견", MessageBoxButtons.YesNo, MessageBoxIcon.Question) ==
                        DialogResult.Yes)
                    {
                        UpdateProgram(restartOsu);
                    }
                }
                else
                {
                    MessageBox.Show("최신 버전입니다!", "야호!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch
            {
                MessageBox.Show("버전 정보를 받아오지 못했습니다.", null, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/sunghwan2789/osu-Lyrics");
        }



        private static void UpdateProgram(bool restartOsu)
        {
            const string url = "http://bloodcat.com/_data/static/lz.zip";

            var current = Application.ExecutablePath;
            var update = Path.GetTempFileName();
            using (var zip = Request.Create(url).GetResponse().GetResponseStream())
            using (var ms = new MemoryStream())
            {
                var buff = new byte[4096];
                if (Program.IntB(buff, 0, zip.Read(buff, 0, 30)) != 0x504B0304) // "PK"
                {
                    throw new Exception();
                }

                var length = Program.Int(buff, 18);
                zip.Read(buff, 0, Program.Int(buff, 26, 2) + Program.Int(buff, 28, 2));
                while (length > 0)
                {
                    var read = zip.Read(buff, 0, buff.Length);
                    length -= read;
                    if (length <= 0)
                    {
                        read -= length;
                    }
                    ms.Write(buff, 0, read);
                }

                ms.Seek(0, SeekOrigin.Begin);
                Program.Extract(new DeflateStream(ms, CompressionMode.Decompress), update);
            }

            // 윈도우는 실행 중인 파일 삭제는 못 하지만 이름 변경은 가능
            File.Move(current, current + _BakExt);
            File.Move(update, current);

            Application.Exit();
            if (restartOsu)
            {
                Osu.Process.Kill();
            }
            Process.Start(current);
        }
    }
}