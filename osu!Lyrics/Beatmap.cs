using System.IO;

namespace osu_Lyrics
{
    internal class Beatmap
    {
        public readonly string Artist;
        public readonly string ArtistUnicode;
        public readonly string Title;
        public readonly string TitleUnicode;

        public Beatmap() {}

        public Beatmap(string osu)
        {
            var currentSection = "";
            foreach (var line in osu.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            {
                // 주석
                if (line.StartsWith("//"))
                {
                    continue;
                }

                if (line.StartsWith("["))
                {
                    currentSection = line.Substring(1, line.IndexOf("]") - 1);
                    continue;
                }

                if (currentSection == "Metadata")
                {
                    var pair = line.Split(new[] { ':' }, 2);
                    // 빈 줄인지 검사..
                    if (pair.Length != 2)
                    {
                        continue;
                    }
                    // if (key in this): from osu!Preview
                    var property = this.GetType().GetProperty(pair[0].Trim());
                    if (property != null)
                    {
                        property.SetValue(this, pair[1].Trim());
                    }
                }
            }
        }

        public static Beatmap Load(string path)
        {
            return new Beatmap(File.ReadAllText(path));
        }
    }
}
