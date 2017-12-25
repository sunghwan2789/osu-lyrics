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
        public void GetLyricAsyncTest()
        {
            var al = new AlsongSource();
            Assert.IsNull(al.GetLyricAsync(new Audio.AudioInfo()).GetAwaiter().GetResult());
            Assert.IsNotNull(al.GetLyricAsync(new Audio.AudioInfo { Beatmap = new Beatmap.BeatmapMetadata { Artist = "아이유", Title = "좋은 날" } }).GetAwaiter().GetResult());
        }

        [TestMethod()]
        public void GetLyric5AsyncTest()
        {
            var al = new AlsongSource();
            Assert.IsNull(al.GetLyric5Async("좋은 날").GetAwaiter().GetResult());
        }

        [TestMethod()]
        public void GetResembleLyric2AsyncTest()
        {
            var al = new AlsongSource();
            Assert.IsNotNull(al.GetResembleLyric2Async("좋은 날", "아이유").GetAwaiter().GetResult());
        }
    }
}