using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace osu_Lyrics
{
    internal class Program
    {
        private static readonly Mutex Mutex = new Mutex(
            true, Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(GuidAttribute), true)[0].ToString());

        [STAThread]
        private static void Main()
        {
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
    }
}