using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsuLiveStatusPanel.ProcessEvent
{
    public class StatusWrapperProcessEvent:ProcessEventBase
    {
        public BeatmapEntry Beatmap { get; set; }
        public string Mods { get; set; }
        public string ShortMods { get; set; }
    }
}
