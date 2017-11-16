using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace osu_Lyrics.IO
{
    static class FileEx
    {
        public static void PreDel(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch // running
            {
                var bak = path + Constants._BakExt;
                PostDel(bak);
                File.Move(path, bak);
            }
        }

        public static void Extract(Stream s, string path)
        {
            PreDel(path);
            using (var fs = File.OpenWrite(path))
            {
                s.CopyTo(fs);
            }
            s.Dispose();
        }

        public static void Move(string src, string dst)
        {
            PreDel(dst);
            File.Move(src, dst);
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
