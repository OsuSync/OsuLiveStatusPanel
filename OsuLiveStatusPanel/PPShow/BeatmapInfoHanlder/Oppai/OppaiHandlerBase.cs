using Newtonsoft.Json;
using OppaiWNet.Wrap;
using OsuLiveStatusPanel.Mods;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsuLiveStatusPanel.PPShow.BeatmapInfoHanlder.Oppai
{
    public class OppaiHandlerBase : BeatmapInfoHandlerBase
    {
        private byte[] beatmap_data_buffer;

        CompatibleOppaiJson pp_instance;

        public override void HandleBaseInfo(Dictionary<string, string> map_info, ref byte[] beatmap_raw_data, Dictionary<string, object> extra)
        {
            base.HandleBaseInfo(map_info, ref beatmap_raw_data, extra);

            beatmap_data_buffer=beatmap_raw_data;
            pp_instance?.Dispose();
            pp_instance=new CompatibleOppaiJson(new Ezpp(beatmap_raw_data));
        }

        public override void HandleExtraData(Dictionary<string, object> extra, Dictionary<string, string> map_info)
        {
            bool first_output = true;

            var mode_str = map_info["mode"];     

            if (int.TryParse(mode_str, out var mode) || (Enum.TryParse(mode_str,out OsuMode m)&&((mode=(int)m)!=-1)))
            {
                foreach (float acc in extra["AccuracyList"] as List<float>)
                {
                    ApplyPPCalculate(mode, (ModsInfo)extra["Mods"], acc);

                    //add pp
                    map_info[$"pp:{acc:F2}%"] = pp_instance.pp.ToString("F2");

                    if (first_output)
                    {
                        first_output = false;
                        var type = pp_instance.GetType();
                        var members = type.GetProperties();

                        foreach (var prop in members)
                        {
                            var val = prop.GetValue(pp_instance);

                            if (val == null)
                                continue;

                            if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(string) || prop.PropertyType.IsEnum)
                                map_info[prop.Name] = val.ToString();
                            else
                                map_info[prop.Name] = $"{val:F2}";
                        }
                    }
                }
            }
            else
                Log.Error($"Unknown mode value:{mode_str}");
            
            if (first_output)
                Log.Warn("No any oppai result output , maybe this beatmap mode isn't osu!std/taiko");
        }

        private void ApplyPPCalculate(int mode, ModsInfo mods, float acc)
        {
            Debug.Assert(pp_instance!=null);
            
            pp_instance.info.Acc=acc;
            pp_instance.info.Mods=(OppaiWNet.Wrap.Mods)(int)mods.Mod;
            pp_instance.info.Mode=mode;

            pp_instance.info.ApplyChange();
        } 
    }
}
