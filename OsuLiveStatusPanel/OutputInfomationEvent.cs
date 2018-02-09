using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sync.Plugins;
using Sync.Tools;

namespace OsuLiveStatusPanel
{
    public class OutputInfomationEvent:IPluginEvent
    {
        public OutputType CurrentOutputType { get; private set; }

        public OutputInfomationEvent(OutputType type)
        {
            CurrentOutputType = type;
        }
    }
}
