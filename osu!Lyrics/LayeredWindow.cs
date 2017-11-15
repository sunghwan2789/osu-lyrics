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
                var cp = base.CreateParams;
                cp.ExStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT;
                return cp;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            UpdateLayer();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            UpdateLayer();
        }

        private Point DestinationPoint = Point.Empty;

        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);
            DestinationPoint = Location;
            UpdateLayer();
        }

        private Size DestinationSize = Size.Empty;

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            DestinationSize = Size;
            UpdateLayer();
        }

        protected Point SourcePoint = Point.Empty;

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

            try
            {
                var hBitmap = IntPtr.Zero;
                using (var bmp = new Bitmap(Width, Height, PixelFormat.Format32bppArgb))
                using (var g = Graphics.FromImage(bmp))
                {
                    Render(g);
                    hBitmap = bmp.GetHbitmap(Color.FromArgb(0));
                }
                var hOldBitmap = SelectObject(hDC, hBitmap);

                UpdateLayeredWindow(Handle, hScreenDC, ref DestinationPoint, ref DestinationSize, hDC, ref SourcePoint, 0, ref BlendFunction, ULW_ALPHA);

                if (hBitmap != IntPtr.Zero)
                {
                    SelectObject(hDC, hOldBitmap);
                    DeleteObject(hBitmap);
                }
            }
            catch { }

            DeleteDC(hDC);
            ReleaseDC(IntPtr.Zero, hScreenDC);
        }

        public virtual void Render(Graphics g) { }
    }
}
