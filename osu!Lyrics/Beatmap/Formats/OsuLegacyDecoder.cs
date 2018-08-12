using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace osu_Lyrics.Beatmap.Formats
{
    internal class OsuLegacyDecoder : BeatmapDecoder
    {
        public static void Register()
        {
            AddDecoder<OsuLegacyDecoder>(TypeCheck);
        }

        private static bool TypeCheck(string header)
        {
            return header.StartsWith(@"osu file format v");
        }

        public OsuLegacyDecoder() { }

        public OsuLegacyDecoder(string header) { }

        private enum Section
        {
            None,
            General,
            Editor,
            Metadata,
            Difficulty,
            Events,
            TimingPoints,
            Colours,
            HitObjects,
            Variables,
        }

        private void handleMetadata(BeatmapMetadata beatmap, string line)
        {
            if (beatmap == null)
            {
                throw new ArgumentNullException(nameof(beatmap));
            }
            if (line == null)
            {
                throw new ArgumentNullException(nameof(line));
            }

            var pair = splitKeyVal(line, ':');

            switch (pair.Key)
            {
                case @"Title":
                    beatmap.Title = pair.Value;
                    break;
                case @"TitleUnicode":
                    beatmap.TitleUnicode = pair.Value;
                    break;
                case @"Artist":
                    beatmap.Artist = pair.Value;
                    break;
                case @"ArtistUnicode":
                    beatmap.ArtistUnicode = pair.Value;
                    break;
                case @"Creator":
                    beatmap.AuthorString = pair.Value;
                    break;
                case @"Source":
                    beatmap.Source = pair.Value;
                    break;
                case @"Tags":
                    beatmap.Tags = pair.Value;
                    break;
            }
        }

        protected override void ParseFile(StreamReader stream, BeatmapMetadata beatmap)
        {
            if (beatmap == null)
            {
                throw new ArgumentNullException(nameof(beatmap));
            }
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            var section = Section.None;

            string line;
            while ((line = stream.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)
                    || line.StartsWith(@"//"))
                {
                    continue;
                }

                if (line.StartsWith(@"[") && line.EndsWith(@"]"))
                {
                    if (!Enum.TryParse(line.Substring(1, line.Length - 2), out section))
                    {
                        //throw new InvalidDataException($@"Unknown osu section {line}");
                        section = Section.None;
                    }
                    continue;
                }

                switch (section)
                {
                    case Section.Metadata:
                        handleMetadata(beatmap, line);
                        break;
                }
            }
        }

        private KeyValuePair<string, string> splitKeyVal(string line, char separator)
        {
            if (line == null)
            {
                throw new ArgumentNullException(nameof(line));
            }

            var split = line.Trim().Split(new[] { separator }, 2);

            return new KeyValuePair<string, string>
            (
                split[0].Trim(),
                split.Length > 1 ? split[1].Trim() : string.Empty
            );
        }
    }
}
