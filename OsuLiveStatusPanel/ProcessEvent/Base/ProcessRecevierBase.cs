using Sync.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsuLiveStatusPanel.ProcessEvent
{
    public abstract class ProcessRecevierBase
    {
        internal BaseEventDispatcher<IPluginEvent> _dispatcher;

        public void RaiseProcessEvent<T>(T sender) where T: ProcessEventBase
        {
            _dispatcher?.RaiseEvent<T>(sender);
        }

        public abstract void OnEventRegister(BaseEventDispatcher<IPluginEvent> EventBus);
    }
}
