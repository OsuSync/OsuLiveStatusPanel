using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsuLiveStatusPanel.ProcessEvent
{
    public class MetadataProcessEvent:ProcessEventBase
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
