using Sync.Plugins;
using Sync;
using Sync.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using static OsuLiveStatusPanel.Languages;

namespace OsuLiveStatusPanel
{
    public class BeatmapInfomationGeneratorPlugin
    {
        Dictionary<OutputConfig, OutputFormatter> ofs;

        Dictionary<string, string> current_data_dic;

        public bool PPShowAllowDumpInfo = false;

        public Dictionary<string,string> CurrentOutputInfo { get => current_data_dic; }

        public BeatmapInfomationGeneratorPlugin(OsuLiveStatusPanelPlugin osuLiveStatusPanel,string config_path)
        {
            if (!File.Exists(config_path))
            {
                Config.CreateDefaultPPShowConfig(config_path);
            }

            LoadConfig(config_path);
            EventInit(osuLiveStatusPanel);
        }

        private void LoadConfig(string config_path)
        {
            Config.LoadPPShowConfig(config_path);
        }

        private void EventInit(OsuLiveStatusPanelPlugin osuLiveStatusPanel)
        {
            List<float> acc_list = new List<float>();

            foreach (var o in Config.Instance.output_list)
            {
                osuLiveStatusPanel.RegisterProcess(new ProcessEvent.FormatOutputProcessRevevier(o.output_file, o.output_format,true));

                acc_list.AddRange(Utils.GetAccuracyArray(o.output_format));
            }

            foreach (var o in Config.Instance.listen_list)
            {
                osuLiveStatusPanel.RegisterProcess(new ProcessEvent.FormatOutputProcessRevevier(o.output_file, o.output_format, false));
            }

            string oppai_path = Config.Instance.oppai;

            osuLiveStatusPanel.RegisterProcess(new ProcessEvent.PPCalculatorOutputProcessReceiver(oppai_path, acc_list));
        }

        /*
        #region DDRP

        public object GetData(string name)
        {
            if (CurrentOutputInfo == null)
                return null;

            if (name != "pp")
            {
                if (CurrentOutputInfo.TryGetValue(name, out string result))
                {
                    if (int.TryParse(result, out var ival))
                        return ival;
                    else if (double.TryParse(result, out var dval))
                        return dval;
                    return result;
                }
            }
            else
            {
                //output pp
                var list = new List<object>();
                foreach (var acc in PP.AccuracyList)
                    if (CurrentOutputInfo.TryGetValue($"pp:{acc:F2}%", out string pp))
                    {
                        if(double.TryParse(pp, out var dval))
                            list.Add(new { acc, pp=dval });
                    }

                return list;
            }

            return null;
        }

        #endregion
        */
    }
}
