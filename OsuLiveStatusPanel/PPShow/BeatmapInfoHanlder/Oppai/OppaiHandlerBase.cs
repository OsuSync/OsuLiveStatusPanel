using Newtonsoft.Json;
using OppaiWNet.Wrap;
using OsuLiveStatusPanel.Mods;
using OsuLiveStatusPanel.PPShow.Oppai;
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

        Ezpp pp_instance;

        public override void HandleBaseInfo(Dictionary<string, string> map_info, ref byte[] beatmap_raw_data, Dictionary<string, object> extra)
        {
            base.HandleBaseInfo(map_info, ref beatmap_raw_data, extra);

            beatmap_data_buffer=beatmap_raw_data;
            pp_instance?.Dispose();
            pp_instance=new Ezpp(beatmap_raw_data);
        }

        public override void HandleExtraData(Dictionary<string, object> extra, Dictionary<string, string> map_info)
        {
            bool init = true;

            foreach (float acc in extra["AccuracyList"] as List<float>)
            {
                ApplyPPCalculate(int.Parse(map_info["mode"]), (ModsInfo)extra["Mods"], acc);

                //add pp
                map_info[$"pp:{acc:F2}%"]=pp_instance.PP.ToString("F2");

                if (init)
                {
                    var type = pp_instance.GetType();
                    var members = type.GetProperties();

                    foreach (var prop in members)
                    {
                        var val = prop.GetValue(pp_instance);

                        if (val==null)
                            continue;

                        if (prop.PropertyType==typeof(int)||prop.PropertyType==typeof(string)||prop.PropertyType.IsEnum)
                            map_info[prop.Name]=val.ToString();
                        else
                            map_info[prop.Name]=$"{val:F2}";
                    }
                }
            }
            
            if (!init)
                Log.Warn("No any oppai result output , maybe this beatmap mode isn't osu!std/taiko");
        }

        private void ApplyPPCalculate(int mode, ModsInfo mods, float acc)
        {
            Debug.Assert(pp_instance!=null);
            
            pp_instance.Acc=acc;
            pp_instance.Mods=(OppaiWNet.Wrap.Mods)(int)mods.Mod;
            pp_instance.Mode=mode;

            pp_instance.ApplyChange();
        } 
    }
}
