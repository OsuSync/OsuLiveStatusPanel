using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sync.Plugins;

namespace OsuLiveStatusPanel.ProcessEvent
{
    public class RestfulAPIGetterReceiver : ProcessRecevierBase
    {
        public Dictionary<string, string> output_data;


        public override void OnEventRegister(BaseEventDispatcher<IPluginEvent> EventBus)
        {
            throw new NotImplementedException();
        }


    }
}
