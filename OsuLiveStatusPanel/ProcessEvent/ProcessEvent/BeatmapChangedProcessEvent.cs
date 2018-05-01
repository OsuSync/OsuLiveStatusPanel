using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsuLiveStatusPanel.ProcessEvent
{
    class BeatmapChangedProcessEvent:ProcessEventBase
    {
        public BeatmapEntry Beatmap { get; set; }
    }
}
