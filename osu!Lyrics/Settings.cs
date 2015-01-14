using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
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
        private static extern int GetPrivateProfileString(string lpAppName,
            string lpKeyName,
            string lpDefault,
            StringBuilder lpReturnedString,
            int nSize,
            string lpFileName);

        [DllImport("kernel32.dll")]
        private static extern int WritePrivateProfileString(string lpAppName,
            string lpKeyName,
            string lpString,
            string lpFileName);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(Keys vKey);

        public static readonly string _Path = Application.ExecutablePath + @".cfg";
        public static readonly string _Port = Path.GetTempPath() + @"\osu!Lyrics.port";
        public static readonly string _Server = Path.GetTempPath() + @"\osu!Lyrics.server";
        public static readonly string _Grave = Path.GetTempPath() + @"\osu!Lyrics\";

        private static string Get(string section, string key)
        {
            var temp = new StringBuilder(255);
            GetPrivateProfileString(section, key, "", temp, temp.Capacity, _Path);
            return temp.ToString();
        }

        private static void Set(string section, string key, string value)
        {
            WritePrivateProfileString(section, key, value, _Path);
        }

        private static int _LineCount = 0x40;
        private static double _Opacity = -1;
        private static int _VerticalAlign = -1;
        private static int _VerticalOffset = -1;
        private static int _HorizontalAlign = -1;
        private static int _HorizontalOffset = -1;
        private static string _FontFamily = null;
        private static int _FontStyle = -1;
        private static int _FontSize = -1;
        private static int _FontColor = -1;
        private static int _BorderWidth = -1;
        private static int _BorderColor = -1;
        private static int _RefreshRate = -1;
        private static int _KeyBackward = -1;
        private static int _KeyForward = -1;
        private static int _KeyToggle = -1;

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

        public new static double Opacity
        {
            get
            {
                try
                {
                    if (_Opacity < 0.0)
                    {
                        _Opacity = Convert.ToDouble(Get("DESIGN", "Opacity"));
                        if (_Opacity < 0.0 || _Opacity > 1.0)
                        {
                            throw new OverflowException();
                        }
                    }
                    else if (_Opacity > 1.0)
                    {
                        throw new OverflowException();
                    }
                }
                catch
                {
                    _Opacity = 1.0;
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

        public static string FontFamily
        {
            get
            {
                if (_FontFamily == null)
                {
                    _FontFamily = Get("DESIGN", "FontFamily");
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
                        if (_FontSize < 0)
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
                return Color.FromArgb(((int) (Opacity * 255) << 24) | _FontColor);
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
                return Color.FromArgb(((int) (Opacity * 255) << 24) | _BorderColor);
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


        private static StringFormat _StringFormat = null;
        private static Font _Font = null;
        private static SolidBrush _Brush = null;
        private static Pen _Border = null;
        private static float _FontSizeInEm = -1;
        private static PointF _DrawingOrigin = new PointF();
        private static float _NoticeFontSizeInEm = -1;

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

        public new static Font Font
        {
            get
            {
                if (_Font == null)
                {
                    _Font = new Font(FontFamily, FontSize, (FontStyle) FontStyle, GraphicsUnit.Point);
                }
                return _Font;
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

        public static float FontSizeInEm
        {
            get
            {
                if (_FontSizeInEm < 0)
                {
                    throw new OverflowException();
                }
                return _FontSizeInEm;
            }
            set { _FontSizeInEm = value; }
        }

        public static PointF DrawingOrigin
        {
            get
            {
                if (_DrawingOrigin.IsEmpty)
                {
                    throw new OverflowException();
                }
                return _DrawingOrigin;
            }
            set { _DrawingOrigin = value; }
        }

        public static float NoticeFontSizeInEm
        {
            get
            {
                if (_NoticeFontSizeInEm < 0)
                {
                    throw new OverflowException();
                }
                return _NoticeFontSizeInEm;
            }
            set { _NoticeFontSizeInEm = value; }
        }


        private Font __Font;
        private Color __FontColor;
        private Color __BorderColor;

        private void Settings_Load(object sender, EventArgs e)
        {
            label17.Text = Application.ProductVersion;
            numericUpDown1.Value = LineCount;
            trackBar1.Value = (int) Math.Floor(Opacity * 100);
            comboBox1.SelectedIndex = (int) VerticalAlign;
            numericUpDown2.Value = Math.Abs(VerticalOffset);
            comboBox2.SelectedIndex = (int) HorizontalAlign;
            numericUpDown3.Value = Math.Abs(HorizontalOffset);
            __Font = Font;
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
            Loaded = true;
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            label3.Text = trackBar1.Value.ToString("#0'%'");
            Update();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            numericUpDown2.Enabled = comboBox1.SelectedIndex != 1;
            Update();
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            numericUpDown3.Enabled = comboBox2.SelectedIndex != 1;
            Update();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            fontDialog1.Font = __Font;
            if (fontDialog1.ShowDialog() == DialogResult.OK)
            {
                __Font = fontDialog1.Font;
                Update();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            colorDialog1.Color = __FontColor;
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                __FontColor = colorDialog1.Color;
                Update();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            colorDialog1.Color = __BorderColor;
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                __BorderColor = colorDialog1.Color;
                Update();
            }
        }

        private void Update(object sender = null, EventArgs e = null)
        {
            if (!Loaded)
            {
                return;
            }

            _LineCount = (int) numericUpDown1.Value;
            _Opacity = trackBar1.Value / 100.0;
            _VerticalAlign = comboBox1.SelectedIndex;
            _VerticalOffset = (int) numericUpDown2.Value;
            _HorizontalAlign = comboBox2.SelectedIndex;
            _HorizontalOffset = (int) numericUpDown3.Value;
            _FontFamily = __Font.Name;
            _FontStyle = (int) __Font.Style;
            _FontSize = (int) Math.Round(__Font.Size);
            _FontColor = __FontColor.ToArgb() & 0xFFFFFF;
            _BorderWidth = (int) numericUpDown4.Value;
            _BorderColor = __BorderColor.ToArgb() & 0xFFFFFF;
            _RefreshRate = (int) numericUpDown5.Value;
            _KeyBackward = (int) textBox1.Tag;
            _KeyForward = (int) textBox2.Tag;
            _KeyToggle = (int) textBox3.Tag;

            UpdateScreen();
        }

        private static void UpdateScreen()
        {
            _StringFormat = null;
            _Font = null;
            _Brush = null;
            _Border = null;
            _FontSizeInEm = -1;
            _DrawingOrigin = new PointF();

            Lyrics.Constructor.Invoke(new MethodInvoker(Lyrics.Constructor.Refresh));
        }

        private void Settings_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.OK)
            {
                Update();

                Set("LAYOUT", "LineCount", _LineCount.ToString());
                Set("DESIGN", "Opacity", _Opacity.ToString("F2"));
                Set("LAYOUT", "VerticalAlign", _VerticalAlign.ToString());
                Set("LAYOUT", "VerticalOffset", _VerticalOffset.ToString());
                Set("LAYOUT", "HorizontalAlign", _HorizontalAlign.ToString());
                Set("LAYOUT", "HorizontalOffset", _HorizontalOffset.ToString());
                Set("DESIGN", "FontFamily", _FontFamily);
                Set("DESIGN", "FontStyle", _FontStyle.ToString());
                Set("DESIGN", "FontSize", _FontSize.ToString());
                Set("DESIGN", "FontColor", _FontColor.ToString("X6"));
                Set("DESIGN", "BorderWidth", _BorderWidth.ToString());
                Set("DESIGN", "BorderColor", _BorderColor.ToString("X6"));
                Set("PROGRAM", "RefreshRate", _RefreshRate.ToString());
                Set("PROGRAM", "KeyBackward", _KeyBackward.ToString());
                Set("PROGRAM", "KeyForward", _KeyForward.ToString());
                Set("PROGRAM", "KeyToggle", _KeyToggle.ToString());
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

                UpdateScreen();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private bool Pressed(Keys key)
        {
            return Convert.ToBoolean(GetAsyncKeyState(key));
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
                        foreach (var j in i.Value.Where(Pressed))
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
            Update();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            const string versionURL = @"http://bloodcat.com/_data/static/lv.txt";
            const string programURL = @"http://bloodcat.com/_data/static/lz.zip";

            try
            {
                using (var wc = new WebClient { Encoding = Encoding.UTF8 })
                {
                    var current = Version.Parse(Application.ProductVersion);
                    var data =
                        wc.DownloadString(versionURL)
                            .Replace("\r\n", "\n")
                            .Replace("\r", "\n")
                            .Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(i => i.Split(new[] { '|' }, 2))
                            .TakeWhile(i => Version.Parse(i[0]) > current)
                            .ToList();
                    data.Reverse();
                    if (data.Count > 0)
                    {
                        var sb = new StringBuilder();
                        sb.Append(current);
                        sb.Append("->");
                        sb.AppendLine(data[0][0]);
                        sb.AppendLine();
                        data.ForEach(i => sb.AppendLine(i[1]));
                        if (
                            MessageBox.Show(
                                sb.ToString().TrimEnd(), @"최신 버전을 내려받을까요?", MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            var dir = Path.GetDirectoryName(_Path);
                            wc.DownloadFile(programURL, dir + @"\osu!Lyrics.zip");
                            if (MessageBox.Show(@"최신 버전을 내려받아 osu!Lyrics.zip으로 저장했습니다.
내려받은 폴더를 열고 프로그램을 종료하시겠습니까?", @"내려받기 완료", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                            {
                                Process.Start(dir);
                                Application.Exit();
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show(@"최신 버전입니다!
기능추가 및 개선요청은 osu! 메세지로 보내주세요.", @"야호!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch
            {
                MessageBox.Show(@"버전 정보를 받아오지 못했습니다.", null, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            Process.Start(@"http://osu.ppy.sh/u/1112529");
        }
    }
}