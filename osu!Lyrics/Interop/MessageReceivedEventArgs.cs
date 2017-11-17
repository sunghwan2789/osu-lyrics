using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace osu_Lyrics.Interop
{
    class MessageReceivedEventArgs
    {
        private string[] dataFetched;

        public MessageReceivedEventArgs(string data)
        {
            dataFetched = data.Split('|');
        }

        public DateTime CreatedTime => DateTime.FromFileTime(long.Parse(dataFetched[0], NumberStyles.HexNumber));

        public string AudioPath => dataFetched[1];

        public double AudioPlayTime => double.Parse(dataFetched[2]);

        public double AudioPlaySpeed => double.Parse(dataFetched[3]);

        public string BeatmapPath => dataFetched[4];
    }
}
