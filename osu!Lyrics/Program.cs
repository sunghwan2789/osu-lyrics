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
                    Task.Factory.StartNew(() => PostDel(Application.ExecutablePath + Settings._BakExt));

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
    }
}