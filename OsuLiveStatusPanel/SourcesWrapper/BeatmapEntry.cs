using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsuLiveStatusPanel
{
    //copy from NowPlaying
    public class BeatmapEntry
    {
        public OutputType OutputType { get; set; } = OutputType.Listen;

        public int BeatmapId { get; set; }
        public int BeatmapSetId { get; set; }

        public string OsuFilePath { get; set; }
    }
}
