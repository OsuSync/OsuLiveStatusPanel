using Sync.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsuLiveStatusPanel
{
    public abstract class SourceWrapperBase<T>: SourceWrapperBase where T : Plugin
    {
        T ref_plugin;

        OsuLiveStatusPanelPlugin ref_panel;

        public T RefPlugin { get => ref_plugin; }

        public OsuLiveStatusPanelPlugin RefPanelPlugin { get => ref_panel; }

        public SourceWrapperBase(T ref_plugin,OsuLiveStatusPanelPlugin plugin)
        {
            this.ref_plugin = ref_plugin;
            this.ref_panel = plugin;
        }
    }

    public abstract class SourceWrapperBase
    {
        public abstract void Detach();
        public abstract bool Attach();
    }
}
