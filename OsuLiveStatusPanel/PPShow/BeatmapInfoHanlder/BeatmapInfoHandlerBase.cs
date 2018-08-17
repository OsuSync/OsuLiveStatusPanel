using OsuLiveStatusPanel.Mods;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsuLiveStatusPanel.PPShow.BeatmapInfoHanlder
{
    public abstract class BeatmapInfoHandlerBase
    {
        public virtual void HandleBaseInfo(Dictionary<string, string> map_info, ref byte[] beatmap_raw_data, Dictionary<string, object> extra)
        {
            var mod = (ModsInfo)extra["Mods"];
            Beatmap.BeatmapParser.ParseBeatmap(ref beatmap_raw_data, map_info, mod);
        }

        public abstract void HandleExtraData(Dictionary<string, object> extra, Dictionary<string, string> map_info);

        private string _TryGetValue(Dictionary<string, string> dic,string key, string default_val = "")
        {
            if (!dic.TryGetValue(key, out string val))
                return default_val;
            return val;
        }

        public virtual void AddExtraBeatmapInfo(Dictionary<string, string> dic)
        {
            dic["beatmap_setlink"] = int.Parse(_TryGetValue(dic,"beatmap_setid", "-1")) > 0 ? (@"https://osu.ppy.sh/s/" + dic["beatmap_setid"]) : "";
            dic["beatmap_link"] = int.Parse(_TryGetValue(dic,"beatmap_id", "-1")) > 0 ? (@"https://osu.ppy.sh/b/" + dic["beatmap_id"]) : string.Empty;

            dic["title_avaliable"] = _TryGetValue(dic,"title_unicode", _TryGetValue(dic,"title", string.Empty));
            dic["artist_avaliable"] = _TryGetValue(dic,"artist_unicode", _TryGetValue(dic,"artist", string.Empty));

            dic["mods"] = dic["mods_str"];
            dic["circles"] = dic["num_circles"];
            dic["spinners"] = dic["num_spinners"];
            dic["sliders"] = dic["num_sliders"];
        }
    }
}
