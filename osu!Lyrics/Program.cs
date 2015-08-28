using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace osu_Lyrics
{
    internal class Program
    {
        [STAThread]
        private static void Main()
        {
            bool createdNew;
            using (new Mutex(true, @"osu!Lyrics v" + Application.ProductVersion, out createdNew))
            {
                if (createdNew)
                {
                    Task.Run(() => PostDel(Application.ExecutablePath + Settings._BakExt));

                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new Lyrics());
                }
                else
                {
                    Osu.Show();
                }
            }
        }

        public static void Extract(Stream s, string path)
        {
            try
            {
                File.Delete(path);
            }
            catch // running
            {
                var bak = path + Settings._BakExt;
                PostDel(bak);
                File.Move(path, bak);
            }
            using (var fs = File.OpenWrite(path))
            {
                s.CopyTo(fs);
            }
            s.Dispose();
        }

        public static void PostDel(string bak)
        {
            while (File.Exists(bak))
            {
                try
                {
                    File.Delete(bak);
                }
                catch
                {
                    Thread.Sleep(1000);
                }
            }
        }

        public static int IntB(byte[] buff, int offset, int len = 4)
        {
            if (len-- > 4)
            {
                len = 3;
            }
            var a = 0;
            for (var i = 0; i <= len; i++)
            {
                a |= buff[offset + i] << 8 * (len - i);
            }
            return a;
        }

        public static long LongB(byte[] buff, int offset, int len = 8)
        {
            if (len > 8)
            {
                len = 8;
            }
            len -= 4;
            return (long) IntB(buff, offset) << 8 * len | IntB(buff, offset + 4, len);
        }

        public static int Int(byte[] buff, int offset, int len = 4)
        {
            var a = 0;
            for (var i = 0; i < len && i < 4; i++)
            {
                a |= buff[offset + i] << 8 * i;
            }
            return a;
        }
    }
}