using Newtonsoft.Json;
using OsuLiveStatusPanel.PPShow;
using Sync.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OsuLiveStatusPanel
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

        private string oppai;

        public InfoOutputter(string oppai, List<float> acc_list)
        {
            AccuracyList = acc_list;

            this.oppai = oppai;
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

            ParseBeatmap(osu_file_path, extra_data, mods, out byte[] beatmap_data, out uint data_length);

            OutputDataMap["mods_str"] = mods.ShortName;

            int nobject = int.Parse(extra_data["num_objects"]);
            uint mode = uint.Parse(extra_data["mode"]);

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

            Oppai.sppv2_by_acc(data, length, acc, mode, (uint)mods.Mod, buffer, length);

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

            string _TryGetValue(string key, string default_val = "")
            {
                if (!dic.TryGetValue(key, out string val))
                    return default_val;
                return val;
            }
        }

        #region ParseBeatmap

        private static readonly Dictionary<string, string> METADATA_MAP = new Dictionary<string, string> {
            {"BeatmapID","beatmap_id"},
            {"Source","source"},
            {"BeatmapSetID","beatmap_setid"},
            {"TitleUnicode","title_unicode"},
            {"ArtistUnicode","artist_unicode"},
            {"Artist","artist"},
            {"Title","title"},
            {"Version","version"},
            {"Tags","tags"},
            {"Creator","creator"}
        };

        private static readonly Dictionary<string, string> DIFFICALUT_MAP = new Dictionary<string, string>
        {
            {"HPDrainRate","hp"},
            {"CircleSize","cs"},
            {"OverallDifficulty","od"},
            {"ApproachRate","ar"}
        };

        private static readonly Dictionary<string, string> GENERAL_MAP = new Dictionary<string, string>
        {
            {"Mode","mode"}
        };

        private const int TYPE_CIRCLE = 1;
        private const int TYPE_SLIDER = 2;
        private const int TYPE_SPINER = 8;

        private bool IS_TYPE(int value, int type) => (type & value) != 0;

        private void ParseBeatmap(string file_path, Dictionary<string, string> extra_data, ModsInfo mods, out byte[] beatmap_data, out uint data_length)
        {
            int status = 0;
            int nobjects = 0, ncircle = 0, nslider = 0, nspiner = 0;
            double min_bpm = int.MaxValue, max_bpm = int.MinValue, current_bpm = 0;

            beatmap_data = File.ReadAllBytes(file_path);
            data_length = (uint)beatmap_data.Length;

            using (StreamReader reader = new StreamReader(new MemoryStream(beatmap_data, false)))
            {
                //简单的状态机
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    switch (status)
                    {
                        case 0: //seeking
                            if (line == "[General]")
                            {
                                status = 5;
                            }

                            if (line == "[Metadata]")
                            {
                                status = 1;
                            }

                            if (line == "[Difficulty]")
                            {
                                //status = 4; //不需要
                            }

                            if (line == "[TimingPoints]")
                            {
                                status = 2;
                            }

                            if (line == "[HitObjects]")
                            {
                                status = 3;
                            }

                            break;

                        case 1: //Metadata
                            if (string.IsNullOrWhiteSpace(line))
                            {
                                status = 0;
                                break;
                            }

                            foreach (var pair in METADATA_MAP)
                                if (line.StartsWith(pair.Key))
                                {
                                    extra_data[pair.Value] = line.Remove(0, pair.Key.Length + 1).Trim();
                                    break;
                                }

                            break;

                        case 5: //General
                            if (string.IsNullOrWhiteSpace(line))
                            {
                                status = 0;
                                break;
                            }

                            foreach (var pair in GENERAL_MAP)
                                if (line.StartsWith(pair.Key))
                                {
                                    extra_data[pair.Value] = line.Remove(0, pair.Key.Length + 1).Trim();
                                    break;
                                }

                            break;

                        case 4: //Difficulty
                            if (string.IsNullOrWhiteSpace(line))
                            {
                                status = 0;
                                break;
                            }

                            foreach (var pair in DIFFICALUT_MAP)
                                if (line.StartsWith(pair.Key))
                                {
                                    extra_data[pair.Value] = line.Remove(0, pair.Key.Length + 1).Trim();
                                    break;
                                }

                            break;

                        case 2: //TimingPoints
                            if (string.IsNullOrWhiteSpace(line))
                            {
                                min_bpm = Math.Round(min_bpm, MidpointRounding.AwayFromZero);
                                max_bpm = Math.Round(max_bpm, MidpointRounding.AwayFromZero);

                                if (mods.HasMod(ModsInfo.Mods.DoubleTime) || mods.HasMod(ModsInfo.Mods.Nightcore))
                                {
                                    min_bpm *= 1.5;
                                    max_bpm *= 1.5;
                                    min_bpm = Math.Round(min_bpm, MidpointRounding.AwayFromZero);
                                    max_bpm = Math.Round(max_bpm, MidpointRounding.AwayFromZero);
                                }
                                if (mods.HasMod(ModsInfo.Mods.HalfTime))
                                {
                                    min_bpm *= 0.75;
                                    max_bpm *= 0.75;
                                    min_bpm = Math.Round(min_bpm, MidpointRounding.AwayFromZero);
                                    max_bpm = Math.Round(max_bpm, MidpointRounding.AwayFromZero);
                                }
#if DEBUG
                                IO.CurrentIO.Write($"[Oppai]BPM:{min_bpm} ~ {max_bpm}");
#endif

                                extra_data["min_bpm"] = min_bpm.ToString();
                                extra_data["max_bpm"] = max_bpm.ToString();

                                status = 0;
                                break;
                            }

                            bool is_red_line = true;
                            string[] data = line.Split(',');

                            if (data.Length >= 7)
                            {
                                if (data[6] == "1" || string.IsNullOrWhiteSpace(data[6]))
                                    is_red_line = true;
                                else
                                    is_red_line = false;
                            }

                            if (!is_red_line) break;//1是红线

                            double val = double.Parse(data[1]);

                            if (val > 0)
                            {
                                val = 60000 / val;
                                current_bpm = val;
                            }
                            else
                            {
                                double mul = Math.Abs(100 + val) / 100.0f;
                                val = current_bpm * (1 + mul);
                                break;
                            }

                            min_bpm = Math.Min(val, min_bpm);
                            max_bpm = Math.Max(val, max_bpm);
                            break;

                        case 3:
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                var obj_data = line.Split(',');

                                if (obj_data.Length >= 4)
                                {
                                    nobjects++;
                                    int type = int.Parse(obj_data[3]);

                                    if (IS_TYPE(type, TYPE_CIRCLE))
                                        ncircle++;
                                    else if (IS_TYPE(type, TYPE_SLIDER))
                                        nslider++;
                                    else if (IS_TYPE(type, TYPE_SPINER))
                                        nspiner++;
                                }
                            }
                            break;

                        default:
                            break;
                    }
                }
            }

            extra_data["num_objects"] = nobjects.ToString();
            extra_data["num_circles"] = ncircle.ToString();
            extra_data["num_sliders"] = nslider.ToString();
            extra_data["num_spinners"] = nspiner.ToString();
        }

        #endregion ParseBeatmap
    }
}