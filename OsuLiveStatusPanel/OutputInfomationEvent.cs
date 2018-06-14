using Sync.Plugins;

namespace OsuLiveStatusPanel
{
    public class OutputInfomationEvent : IPluginEvent
    {
        public OutputType CurrentOutputType { get; private set; }

        public OutputInfomationEvent(OutputType type)
        {
            CurrentOutputType = type;
        }
    }
}