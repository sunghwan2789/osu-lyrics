using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static osu_Lyrics.Interop.NativeMethods;

namespace osu_Lyrics
{
    [System.ComponentModel.DesignerCategory("code")]
    class LayeredWindow : Form
    {
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

        public new FormBorderStyle FormBorderStyle { get; } = FormBorderStyle.None;

        public LayeredWindow() : base()
        {
            base.FormBorderStyle = FormBorderStyle.None;
        }

        protected override void OnLoad(EventArgs e)
        {
            UpdateLayer();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            UpdateLayer();
        }

        protected BLENDFUNCTION BlendFunction = new BLENDFUNCTION
        {
            BlendOp = AC_SRC_OVER,
            BlendFlags = 0,
            SourceConstantAlpha = 255,
            AlphaFormat = AC_SRC_ALPHA
        };

        protected void UpdateLayer()
        {
            var hScreenDC = GetDC(IntPtr.Zero);
            var hDC = CreateCompatibleDC(hScreenDC);
            var hBitmap = IntPtr.Zero;
            var hOldBitmap = IntPtr.Zero;

            Bitmap bmp = null;
            Graphics g = null;
            try
            {
                bmp = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
                g = Graphics.FromImage(bmp);

                Render(g);
                hBitmap = bmp.GetHbitmap(Color.FromArgb(0));
                hOldBitmap = SelectObject(hDC, hBitmap);

                var ptDst = Location;
                var sizeDst = Size;
                var ptSrc = Point.Empty;
                UpdateLayeredWindow(Handle, hScreenDC, ref ptDst, ref sizeDst, hDC, ref ptSrc, 0, ref BlendFunction, ULW_ALPHA);
            }
            catch { }
            finally
            {
                g?.Dispose();
                bmp?.Dispose();
            }

            if (hBitmap != IntPtr.Zero)
            {
                SelectObject(hDC, hOldBitmap);
                DeleteObject(hBitmap);
            }
            DeleteDC(hDC);
            ReleaseDC(IntPtr.Zero, hScreenDC);
        }

        public virtual void Render(Graphics g)
        {
        }
    }
}
