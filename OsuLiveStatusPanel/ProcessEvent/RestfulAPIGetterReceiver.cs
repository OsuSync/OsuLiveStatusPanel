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
        private Dictionary<string, string> output_data;

        public override void OnEventRegister(BaseEventDispatcher<IPluginEvent> EventBus)
        {
            EventBus.BindEvent<ClearProcessEvent>(e => output_data = null);

            EventBus.BindEvent<MetadataProcessEvent>(e =>
            {
                if (output_data == null)
                    output_data = new Dictionary<string, string>();
                output_data[e.Name] = e.Value;
            });

            EventBus.BindEvent<PackedMetadataProcessEvent>(e =>
            {
                if (output_data == null)
                    output_data = new Dictionary<string, string>(e.OutputData);
                else
                    foreach (var pair in e.OutputData)
                        output_data[pair.Key] = pair.Value;
            });
        }

        public string GetData(string name)
        {
            if (output_data == null)
                return null;
            if (output_data.TryGetValue(name,out string result))
                return result;
            return null;
        }
    }
}
