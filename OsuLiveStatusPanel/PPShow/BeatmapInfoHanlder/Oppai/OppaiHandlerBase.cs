using Newtonsoft.Json;
using OsuLiveStatusPanel.Mods;
using OsuLiveStatusPanel.PPShow.Oppai;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsuLiveStatusPanel.PPShow.BeatmapInfoHanlder.Oppai
{
    public class OppaiHandlerBase : BeatmapInfoHandlerBase
    {
        private byte[] buffer = new byte[4096];

        private byte[] beatmap_data_buffer;
        uint data_length;

        public override void HandleBaseInfo(Dictionary<string, string> map_info, ref byte[] beatmap_raw_data, Dictionary<string, object> extra)
        {
            base.HandleBaseInfo(map_info, ref beatmap_raw_data, extra);

            beatmap_data_buffer = beatmap_raw_data;
        }

        public override void HandleExtraData(Dictionary<string, object> extra, Dictionary<string, string> map_info)
        {
            bool init = true;
            
            foreach (float acc in extra["AccuracyList"] as List<float>)
            {
                var oppai_result = GetOppaiResult(beatmap_data_buffer, (uint)beatmap_data_buffer.Length, uint.Parse(map_info["mode"]), (ModsInfo)extra["Mods"] , acc);

                if (oppai_result != null)
                {
                    //add pp
                    map_info[$"pp:{acc:F2}%"] = oppai_result.pp.ToString("F2");

                    if (init)
                    {
                        var oppai_json = oppai_result;
                        var type = oppai_json.GetType();
                        var members = type.GetProperties();

                        foreach (var prop in members)
                        {
                            var val = prop.GetValue(oppai_json);

                            if (val == null)
                                continue;

                            if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(string))
                                map_info[prop.Name] = val.ToString();
                            else
                                map_info[prop.Name] = $"{val:F2}";
                        }
                    }
                }
            }
            
            if (!init)
                Log.Warn("No any oppai result output , maybe this beatmap mode isn't osu!std/taiko");
        }

        private OppaiJson GetOppaiResult(byte[] data, uint length, uint mode, ModsInfo mods, double acc)
        {
            Array.Clear(buffer, 0, buffer.Length);

            PPShow.Oppai.Oppai.sppv2_by_acc(data, length, acc, mode, (uint)mods.Mod, buffer, length);

            string content = Encoding.UTF8.GetString(buffer).TrimEnd('\0')/*.Replace("\0",string.Empty)*/;

            if (!content.Contains("no error"))
            {
                return null;
            }

            var oppai_result = JsonConvert.DeserializeObject<OppaiJson>(content);

            return oppai_result;
        } 
    }
}
