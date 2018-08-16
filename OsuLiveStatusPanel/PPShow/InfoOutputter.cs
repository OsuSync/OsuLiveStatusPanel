using Newtonsoft.Json;
using OsuLiveStatusPanel.Mods;
using OsuLiveStatusPanel.PPShow.Beatmap;
using OsuLiveStatusPanel.PPShow.Oppai;
using OsuLiveStatusPanel.PPShow.Oppai.CTB;
using OsuLiveStatusPanel.PPShow.Oppai.Mania;
using Sync.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace OsuLiveStatusPanel.PPShow
{
    internal class InfoOutputter
    {
        private static readonly ModsInfo.Mods[] OPPAI_SUPPORT_MODS = new[] { ModsInfo.Mods.NoFail, ModsInfo.Mods.Easy, ModsInfo.Mods.Hidden, ModsInfo.Mods.HardRock, ModsInfo.Mods.DoubleTime, ModsInfo.Mods.HalfTime, ModsInfo.Mods.Nightcore, ModsInfo.Mods.Flashlight, ModsInfo.Mods.SpunOut };

        private ManiaPPCalculator maniaPPCalculator;
        private CTBPPCalculator ctbPPCalculator;

        Stopwatch sw = new Stopwatch();

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

            if (CheckExsitRealtimePPPlugin())
            {
                ctbPPCalculator = new CTBPPCalculator();
                Log.Output("find RealtimePP plugin,OLSP is support ctb pp calculating as well.");

                maniaPPCalculator = new ManiaPPCalculator();
                Log.Output("find RealtimePP plugin,OLSP is support mania pp calculating as well.");
            }
        }

        private bool CheckExsitRealtimePPPlugin() => Sync.SyncHost.Instance.EnumPluings().Any(plugin => plugin.Name == "RealTimePPDisplayer");

        public bool TrigOutput(OutputType output_type, string osu_file_path, ModsInfo mods, params KeyValuePair<string, object>[] extra)
        {
            List<OppaiJson> oppai_infos = new List<OppaiJson>();

            Dictionary<string, string> OutputDataMap = new Dictionary<string, string>();

            if (extra != null)
            {
                foreach (var data in extra)
                {
                    OutputDataMap[data.Key] = data.Value.ToString();
                }
            }

            string osu_file = osu_file_path;

            BeatmapParser.ParseBeatmap(osu_file_path, OutputDataMap, mods, out byte[] beatmap_data, out uint data_length);

            OutputDataMap["mods_str"] = mods.ShortName;

            int nobject = int.Parse(OutputDataMap["num_objects"]);
            uint mode = uint.Parse(OutputDataMap.TryGetValue("mode", out string _m) ? _m : "0");//没有那就默认0

            if (!string.IsNullOrWhiteSpace(mods.ShortName))
            {
                mods = FilteVailedMod(mods);
            }

            if (mode <= 1)
            {
                foreach (float acc in AccuracyList)
                {
                    var oppai_result = GetOppaiResult(beatmap_data, data_length, mode, mods, acc);

                    if (oppai_result != null)
                    {
                        oppai_infos.Add(oppai_result);
                        //add pp
                        OutputDataMap[$"pp:{acc:F2}%"] = oppai_result.pp.ToString("F2");
                    }
                }

                #region Get base beatmap info from oppai once

                if (oppai_infos.Count != 0)
                {
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
                }
                else
                {
                    Log.Warn("No any oppai result output , maybe this beatmap mode isn't osu!std/taiko");
                }

                #endregion Get base beatmap info from oppai once
            }
            else if (mode == 3 && maniaPPCalculator != null)//是否为mania铺面且初始化mania屁屁计算器
            {
                //先钦定好beatmap以及mod
                OsuRTDataProvider.BeatmapInfo.Beatmap beatmap = extra.Where(p => p.Key == "ortdp_beatmap").FirstOrDefault().Value as OsuRTDataProvider.BeatmapInfo.Beatmap;

                maniaPPCalculator.SetMod(mods);
                maniaPPCalculator.SetBeatmap(beatmap);

#if DEBUG
                IO.CurrentIO.WriteColor($"[OLSP]will calculate mania:{beatmap?.Artist} - {beatmap?.Title}[{beatmap?.Difficulty}]", ConsoleColor.Cyan);
#endif

                foreach (float acc in AccuracyList)
                {
                    var pp = maniaPPCalculator.Calculate(acc);

                    if (pp.HasValue)
                        OutputDataMap[$"pp:{acc:F2}%"] = pp.Value.ToString("F2");
                }
            }
            else if (mode == 2 && ctbPPCalculator != null)//ctb
            {
                sw.Restart();

                //先钦定好beatmap以及mod
                OsuRTDataProvider.BeatmapInfo.Beatmap beatmap = extra.Where(p => p.Key == "ortdp_beatmap").FirstOrDefault().Value as OsuRTDataProvider.BeatmapInfo.Beatmap;

                ctbPPCalculator.SetMod(mods);
                ctbPPCalculator.SetBeatmap(beatmap);

#if DEBUG
                IO.CurrentIO.WriteColor($"[OLSP]will calculate ctb:{beatmap?.Artist} - {beatmap?.Title}[{beatmap?.Difficulty}]", ConsoleColor.Cyan);
#endif

                foreach (float acc in AccuracyList)
                {
                    var pp = ctbPPCalculator.Calculate(acc);

                    if (pp.HasValue)
                        OutputDataMap[$"pp:{acc:F2}%"] = pp.Value.ToString("F2");
                }

                IO.CurrentIO.WriteColor($"[OLSP]ctb calculate pp:{sw.ElapsedMilliseconds}ms", ConsoleColor.Cyan);
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

            if (!content.Contains("no error"))
            {
                return null;
            }

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