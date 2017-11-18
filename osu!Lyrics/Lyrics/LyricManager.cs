using osu_Lyrics.Interop;
using osu_Lyrics.Lyrics.Sources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace osu_Lyrics.Lyrics
{
    class LyricManager
    {
        private LyricLine currentLine = new LyricLine();
        private Lyric lyric = new Lyric();
        public Lyric Lyric
        {
            get { return lyric; }
            set
            {
                lyric = value;
                LyricSync = 0;
                LyricChanged?.Invoke(null, null);
            }
        }
        private string status = "선곡하세요";

        private static double Now => new TimeSpan(DateTime.Now.Ticks).TotalSeconds;

        /// <summary>
        /// TODO when lyric reset, sync reset
        /// </summary>
        private double lyricSync = 0;
        public double LyricSync
        {
            get { return lyricSync; }
            set
            {
                lyricSync = value;
                currentLine = Lyric.FirstOrDefault() ?? new LyricLine();
            }
        }

        public double PlayTimeElapsed
        {
            get
            {
                var elapsedTime = (Now - playTimeChanged) * PlaySpeed;
                return PlayTime + elapsedTime - LyricSync;
            }
        }

        private double playTimeChanged = 0;
        private double playTime = 0;
        public double PlayTime
        {
            get { return playTime; }
            set
            {
                playTime = value;
                playTimeChanged = Now;
                currentLine = Lyric.FirstOrDefault() ?? new LyricLine();
                PlayTimeChanged?.Invoke(null, null);
            }
        }

        private double playSpeed = 0;
        public double PlaySpeed
        {
            get { return playSpeed; }
            set
            {
                playSpeed = value;
                PlaySpeedChanged?.Invoke(null, null);
            }
        }

        public string AudioPath { get; set; }
        public Audio.AudioInfo AudioInfo { get; set; }
        public Beatmap.BeatmapMetadata BeatmapMetadata { get; set; }

        public LyricManager()
        {
            Osu.MessageReceived += Osu_MessageReceived;
        }

        ~LyricManager()
        {
            Osu.MessageReceived -= Osu_MessageReceived;
        }

        public event EventHandler AudioChanged;
        public event EventHandler LyricChanged;
        public event EventHandler PlayTimeChanged;
        public event EventHandler PlaySpeedChanged;

        private void Osu_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            // 재생 중인 곡이 바꼈다!
            if (e.AudioPath != AudioPath)
            {
                AudioPath = e.AudioPath;
                using (var fs = new FileStream(e.AudioPath, FileMode.Open, FileAccess.Read))
                {
                    AudioInfo = Audio.Formats.AudioDecoder.GetDecoder(fs)?.Decode(fs);
                }
                using (var sr = new StreamReader(e.BeatmapPath))
                {
                    BeatmapMetadata = Beatmap.Formats.BeatmapDecoder.GetDecoder(sr)?.Decode(sr);
                }

                FetchLyric();
            }
            PlayTime = DateTime.Now.Subtract(e.CreatedTime).TotalSeconds + e.AudioPlayTime;
            PlaySpeed = 1 + e.AudioPlaySpeed / 100;
        }

        private CancellationTokenSource cts;
        private void FetchLyric()
        {
            if (cts != null)
            {
                cts.Cancel();
                return;
            }

            cts = new CancellationTokenSource();
            status = "가사 받는 중...";
            Lyric.Clear();
            AudioChanged?.BeginInvoke(null, null, null, null);
            Task.Run(async () =>
            {
                Lyric ret = null;
                foreach (var source in LyricSource.sources)
                {
                    cts.Token.ThrowIfCancellationRequested();
                    ret = await source.GetLyricAsync(AudioInfo);
                    if (ret != null)
                    {
                        break;
                    }
                }

                ret?.Insert(0, new LyricLine());

                cts.Token.ThrowIfCancellationRequested();
                Lyric = ret ?? throw new Exception();
            }, cts.Token).ContinueWith(result =>
            {
                cts = null;
                if (result.IsCanceled)
                {
                    FetchLyric();
                }
                else if (result.IsFaulted)
                {
                    status = "가사 없음";
                    lyric.Clear();
                }
            });
        }

        public Lyric GetLyric()
        {
            if (!lyric.Any())
            {
                return new Lyric
                {
                    new LyricLine(0, status)
                };
            }

            var ret = new Lyric();
            var end = Lyric.SkipWhile(i => i.Time < PlayTimeElapsed).FirstOrDefault();
            foreach (var line in Lyric.SkipWhile(i => i.Time < currentLine.Time))
            {
                if (end != null && line.Time >= end.Time)
                {
                    break;
                }
                if (line.Time > currentLine.Time)
                {
                    currentLine = line;
                    ret.Clear();
                }
                ret.Add(line);
            }
            return ret;
        }
    }
}
