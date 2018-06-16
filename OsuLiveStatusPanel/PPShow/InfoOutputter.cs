using Newtonsoft.Json;
using OsuLiveStatusPanel.Mods;
using OsuLiveStatusPanel.PPShow.Beatmap;
using OsuLiveStatusPanel.PPShow.Oppai;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OsuLiveStatusPanel.PPShow
{
    internal class InfoOutputter
    {
        private static readonly ModsInfo.Mods[] OPPAI_SUPPORT_MODS = new[] { ModsInfo.Mods.NoFail, ModsInfo.Mods.Easy, ModsInfo.Mods.Hidden, ModsInfo.Mods.HardRock, ModsInfo.Mods.DoubleTime, ModsInfo.Mods.HalfTime, ModsInfo.Mods.Nightcore, ModsInfo.Mods.Flashlight, ModsInfo.Mods.SpunOut };

        private static ModsInfo FilteVailedMod(ModsInfo mods)
        {
            ModsInfo result = default(ModsInfo);

            foreach (var vaild_mod in (from mod in OPPAI_SUPPORT_MODS where mods.HasMod(mod) select mod))
                result.Mod |= vaild_mod;

            return result;
        }

        public List<float> AccuracyList;

        public delegate void OnOutputFunc(OutputType output_type, Dictionary<string, string> data_dic);

        public event OnOutputFunc OnOutputEvent;

        public InfoOutputter(List<float> acc_list)
        {
            AccuracyList = acc_list;
        }

        public bool TrigOutput(OutputType output_type, string osu_file_path, ModsInfo mods, params KeyValuePair<string, string>[] extra)
        {
            List<OppaiJson> oppai_infos = new List<OppaiJson>();

            Dictionary<string, string> extra_data = new Dictionary<string, string>();

            if (extra != null)
            {
                foreach (var data in extra)
                {
                    extra_data[data.Key] = data.Value;
                }
            }

            Dictionary<string, string> OutputDataMap = new Dictionary<string, string>();

            string osu_file = osu_file_path;

            BeatmapParser.ParseBeatmap(osu_file_path, extra_data, mods, out byte[] beatmap_data, out uint data_length);

            OutputDataMap["mods_str"] = mods.ShortName;

            int nobject = int.Parse(extra_data["num_objects"]);
            uint mode = uint.Parse(extra_data.TryGetValue("mode",out string _m)?_m:"0");//没有那就默认0

            if (!string.IsNullOrWhiteSpace(mods.ShortName))
            {
                mods = FilteVailedMod(mods);
            }

            foreach (float acc in AccuracyList)
            {
                var oppai_result = GetOppaiResult(beatmap_data, data_length, mode, mods, acc);

                if (oppai_result != null)
                {
                    oppai_infos.Add(oppai_result);
                }

                //add pp
                OutputDataMap[$"pp:{acc:F2}%"] = oppai_result.pp.ToString();
            }

            #region GetBaseInfo

            if (oppai_infos.Count == 0)
            {
                return false;
            }

            var oppai_json = oppai_infos.First();
            var type = oppai_json.GetType();
            var members = type.GetProperties();

            foreach (var prop in members)
            {
                var val = prop.GetValue(oppai_json);

                if (val == null)
                {
                    continue;
                }

                if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(string))
                    OutputDataMap[prop.Name] = val.ToString();
                else
                    OutputDataMap[prop.Name] = $"{val:F2}";
            }

            #endregion GetBaseInfo

            //add extra info(shortcut arguments)
            foreach (var pair in extra_data)
            {
                OutputDataMap[pair.Key] = pair.Value;
            }

            AddExtraInfomation(OutputDataMap);

            OnOutputEvent?.Invoke(output_type, OutputDataMap);

            return true;
        }

        private byte[] buffer = new byte[4096];

        private OppaiJson GetOppaiResult(byte[] data, uint length, uint mode, ModsInfo mods, double acc)
        {
            Array.Clear(buffer, 0, buffer.Length);

            Oppai.Oppai.sppv2_by_acc(data, length, acc, mode, (uint)mods.Mod, buffer, length);

            string content = Encoding.UTF8.GetString(buffer).TrimEnd('\0')/*.Replace("\0",string.Empty)*/;

            var oppai_result = JsonConvert.DeserializeObject<OppaiJson>(content);

            return oppai_result;
        }

        private void AddExtraInfomation(Dictionary<string, string> dic)
        {
            dic["beatmap_setlink"] = int.Parse(_TryGetValue("beatmap_setid", "-1")) > 0 ? (@"https://osu.ppy.sh/s/" + dic["beatmap_setid"]) : "";
            dic["beatmap_link"] = int.Parse(_TryGetValue("beatmap_id", "-1")) > 0 ? (@"https://osu.ppy.sh/b/" + dic["beatmap_id"]) : string.Empty;

            dic["title_avaliable"] = _TryGetValue("title_unicode", _TryGetValue("title", string.Empty));
            dic["artist_avaliable"] = _TryGetValue("artist_unicode", _TryGetValue("artist", string.Empty));

            dic["mods"] = dic["mods_str"];
            dic["circles"] = dic["num_circles"];
            dic["spinners"] = dic["num_spinners"];
            dic["sliders"] = dic["num_sliders"];

            string _TryGetValue(string key, string default_val = "")
            {
                if (!dic.TryGetValue(key, out string val))
                    return default_val;
                return val;
            }
        }
    }
}