using Microsoft.VisualStudio.TestTools.UnitTesting;
using osu_Lyrics.Lyrics;
using osu_Lyrics.Lyrics.Sources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace osu_Lyrics.Lyrics.Sources.Tests
{
    [TestClass]
    public class LyricSourceTests
    {
        [TestMethod]
        public async Task GetLyricsAsyncTest()
        {
            var au = new Audio.AudioInfo { CheckSum = "좋은 날", Beatmap = new Beatmap.BeatmapMetadata { Artist = "아이유", Title = "좋은 날" } };
            var a = LyricSource.GetLyricsAsync(au);
            Lyric ret;
            int inc = 0;
            foreach (var lyricTask in a)
            {
                try
                {
                    ret = await lyricTask;
                    break;
                }
                catch { }
                inc++;
            }
            Assert.IsTrue(inc > 0);
        }
    }
}