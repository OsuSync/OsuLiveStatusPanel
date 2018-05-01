using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsuLiveStatusPanel.ProcessEvent
{
    class PackedMetadataProcessEvent:ProcessEventBase
    {
        public Dictionary<string, string> OutputData { get; set; }
    }
}
