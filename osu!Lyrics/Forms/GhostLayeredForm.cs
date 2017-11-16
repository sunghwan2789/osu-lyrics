using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static osu_Lyrics.Interop.NativeMethods;

namespace osu_Lyrics.Forms
{
    class GhostLayeredForm : LayeredForm
    {
        public GhostLayeredForm()
        {
            ShowInTaskbar = false;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= WS_EX_NOACTIVATE | WS_EX_TRANSPARENT;
                return cp;
            }
        }
    }
}
