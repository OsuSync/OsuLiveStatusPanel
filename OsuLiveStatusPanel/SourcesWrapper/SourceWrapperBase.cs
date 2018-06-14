using Sync.Plugins;

namespace OsuLiveStatusPanel
{
    public abstract class SourceWrapperBase<T> : SourceWrapperBase where T : Plugin
    {
        private T ref_plugin;

        private OsuLiveStatusPanelPlugin ref_panel;

        public T RefPlugin { get => ref_plugin; }

        public OsuLiveStatusPanelPlugin RefPanelPlugin { get => ref_panel; }

        public SourceWrapperBase(T ref_plugin, OsuLiveStatusPanelPlugin plugin)
        {
            this.ref_plugin = ref_plugin;
            this.ref_panel = plugin;
        }
    }

    public abstract class SourceWrapperBase
    {
        public abstract void Detach();

        public abstract bool Attach();

        public OutputType CurrentOutputType { get; protected set; } = OutputType.Listen;
    }
}