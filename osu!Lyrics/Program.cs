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
            using (new Mutex(true, Constants._MutexName, out bool createdNew))
            {
                Osu.Show();
                if (createdNew)
                {
                    // 업데이트 전의 파일 삭제
                    Task.Run(() => FileEx.PostDel(Application.ExecutablePath + Constants._BakExt));

                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new Lyrics());
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