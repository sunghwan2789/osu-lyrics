using Microsoft.VisualStudio.TestTools.UnitTesting;
using osu_Lyrics.Beatmap.Formats;
using osu_Lyrics.Lyrics.Sources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace osu_Lyrics.Lyrics.Sources.Tests
{
    [TestClass()]
    public class AlsongSourceTests
    {
        [TestMethod()]
        public void RegisterTest()
        {
        }

        [TestMethod()]
        public void GetLyric5AsyncTest()
        {
            var al = new AlsongGetLyric5();
            var au = new Audio.AudioInfo { CheckSum = "좋은 날" };
            try
            {
                al.GetLyricAsync(au).GetAwaiter().GetResult();
                Assert.Fail();
            }
            catch (IndexOutOfRangeException) { }
        }

        [TestMethod()]
        public void GetResembleLyric2AsyncTest()
        {
            var al = new AlsongGetResembleLyric2();
            var au = new Audio.AudioInfo { Beatmap = new Beatmap.BeatmapMetadata { Artist = "아이유", Title = "좋은 날" } };
            try
            {
                Assert.IsNotNull(al.GetLyricAsync(au).GetAwaiter().GetResult());
            }
            catch (IndexOutOfRangeException) { }
        }
    }
}