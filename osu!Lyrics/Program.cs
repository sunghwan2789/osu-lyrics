using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace osu_Lyrics
{
    internal class Program
    {
        public static readonly Mutex Mutex = new Mutex(
            true, Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(GuidAttribute), true)[0].ToString());

        [STAThread]
        private static void Main()
        {
            var oldVer = Application.ExecutablePath + @".del";
            while (File.Exists(oldVer))
            {
                try
                {
                    File.Delete(oldVer);
                }
                catch
                {
                    Thread.Sleep(1000);
                }
            }

            if (Mutex.WaitOne(TimeSpan.Zero, true))
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Lyrics());
                Mutex.ReleaseMutex();
            }
            else
            {
                Osu.Show();
            }
        }

        public static bool Extract(string res, string dst)
        {
            try
            {
                Directory.Delete(Settings._Grave, true);
            }
            catch {}
            try
            {
                Directory.CreateDirectory(Settings._Grave);
            }
            catch {}
            try
            {
                if (File.Exists(dst))
                {
                    File.Move(dst, Settings._Grave + Path.GetRandomFileName());
                }
                using (var src = Assembly.GetExecutingAssembly().GetManifestResourceStream(res))
                using (var dest = new FileStream(dst, FileMode.Create))
                {
                    src.CopyTo(dest);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}