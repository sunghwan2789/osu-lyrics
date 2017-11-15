using osu_Lyrics.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace osu_Lyrics.Formats
{
    using Predicate = Predicate<Stream>;
    internal abstract class AudioDecoder
    {
        private static readonly List<Tuple<Predicate, Type>> decoders = new List<Tuple<Predicate, Type>>();

        static AudioDecoder()
        {
            MpegDecoder.Register();
            OggDecoder.Register();
        }

        public static AudioDecoder GetDecoder(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            var decoder = decoders.FirstOrDefault(t => t.Item1.Invoke(stream))?.Item2;

            if (decoder == null)
            {
                throw new IOException(@"Unknown file format");
            }
            return (AudioDecoder)Activator.CreateInstance(decoder, true);
        }

        protected static void AddDecoder<T>(Predicate predict) where T : AudioDecoder
        {
            decoders.Add(new Tuple<Predicate, Type>(predict, typeof(T)));
        }

        public virtual Audio Decode(Stream stream)
        {
            return ParseFile(stream);
        }

        public virtual void Decode(Stream stream, Audio audio)
        {
            ParseFile(stream, audio);
        }

        protected virtual Audio ParseFile(Stream stream)
        {
            var audio = new Audio();

            ParseFile(stream, audio);
            return audio;
        }

        protected abstract void ParseFile(Stream stream, Audio audio);

        protected static string GetCheckSum(Stream s)
        {
            var buff = new byte[0x28000];
            var read = s.Read(buff, 0, buff.Length);
            return string.Join("", MD5.Create().ComputeHash(buff, 0, read).Select(i => i.ToString("x2")));
        }
    }
}
