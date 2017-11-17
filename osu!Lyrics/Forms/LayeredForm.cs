using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static osu_Lyrics.Interop.NativeMethods;

namespace osu_Lyrics.Forms
{
    [System.ComponentModel.DesignerCategory("code")]
    class LayeredForm : Form
    {
        public LayeredForm()
        {
            FormBorderStyle = FormBorderStyle.None;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= WS_EX_LAYERED;
                return cp;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            UpdateWindow();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            UpdateWindow();
        }

        private Point DestinationPoint = Point.Empty;

        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);
            DestinationPoint = Location;
            UpdateWindow();
        }

        private Size DestinationSize = Size.Empty;

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            DestinationSize = Size;
            UpdateWindow();
        }

        protected Point SourcePoint = Point.Empty;

        protected BLENDFUNCTION BlendFunction = new BLENDFUNCTION
        {
            BlendOp = AC_SRC_OVER,
            BlendFlags = 0,
            SourceConstantAlpha = 255,
            AlphaFormat = AC_SRC_ALPHA
        };

        protected void UpdateWindow()
        {
            // スクリーンのGraphicsと、hdcを取得
            var gScreen = Graphics.FromHwnd(IntPtr.Zero);
            var hdcScreen = gScreen.GetHdc();

            // BITMAPのGraphicsと、hdcを取得
            var bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
            var gBitmap = Graphics.FromImage(bitmap);
            Render(gBitmap);
            var hdcBitmap = gBitmap.GetHdc();

            // BITMAPのhdcで、サーフェイスのBITMAPを選択する
            // このとき背景を無色透明にしておく
            var hOldBitmap = SelectObject(hdcBitmap, bitmap.GetHbitmap(Color.FromArgb(0)));

            // レイヤードウィンドウの設定
            UpdateLayeredWindow(
                Handle, hdcScreen, ref DestinationPoint, ref DestinationSize,
                hdcBitmap, ref SourcePoint, 0, ref BlendFunction, ULW_ALPHA);

            // 後始末
            gScreen.ReleaseHdc(hdcScreen);
            gScreen.Dispose();
            DeleteObject(SelectObject(hdcBitmap, hOldBitmap));
            gBitmap.ReleaseHdc(hdcBitmap);
            gBitmap.Dispose();
            bitmap.Dispose();
        }

        public virtual void Render(Graphics g) { }
    }
}
