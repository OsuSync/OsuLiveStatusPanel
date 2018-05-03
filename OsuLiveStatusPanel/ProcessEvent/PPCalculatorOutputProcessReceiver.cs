using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Sync.Plugins;
using Sync.Tools;
using static OsuLiveStatusPanel.Languages;

namespace OsuLiveStatusPanel.ProcessEvent
{
    class PPCalculatorOutputProcessReceiver : ProcessRecevierBase
    {
        static readonly string[] OPPAI_SUPPORT_MODS = new[] { "NF", "EZ", "HD", "HR", "DT", "HT", "NC", "FL", "SO" };

        public List<float> AccuracyList;

        Process p = null;

        Stopwatch sw;

        string oppai;
        
        public PPCalculatorOutputProcessReceiver(string oppai_path,List<float> acc_list)
        {
            AccuracyList = acc_list;

            this.oppai = oppai_path;

            p = new Process();
            p.StartInfo.FileName = oppai;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            p.StartInfo.StandardErrorEncoding = Encoding.UTF8;

            sw = new Stopwatch();
        }

        public override void OnEventRegister(BaseEventDispatcher<IPluginEvent> EventBus)
        {
            EventBus.BindEvent<StatusWrapperProcessEvent>(OnChangeBeatmap);
        }

        public void OnChangeBeatmap(StatusWrapperProcessEvent param)
        {
            if (string.IsNullOrWhiteSpace(param?.Beatmap?.OsuFilePath))
            {
                return;
            }

            CalculatePP(param);
        }

        public void CalculatePP(StatusWrapperProcessEvent param)
        {
            sw.Restart();

            Dictionary<string, string> extra_data = new Dictionary<string, string>();

            Dictionary<string, string> OutputDataMap = new Dictionary<string, string>();

            List<OppaiJson> oppai_infos = new List<OppaiJson>();

            string oppai_cmd;

            string osu_file = param.Beatmap.OsuFilePath;

            string raw_mod_list = param.ShortMods;

            if (raw_mod_list == "None")
                raw_mod_list = "";

            AddExtraInfomationFromBeatmapFile(osu_file, extra_data, raw_mod_list);

            if(param.Beatmap.OutputType == OutputType.Play)
            {
                string mods_str = string.Empty;

                if (!string.IsNullOrWhiteSpace(raw_mod_list))
                {
                    mods_str = String.Join(",", raw_mod_list.Split(',').Where(s => OPPAI_SUPPORT_MODS.Contains(s)));
                }

                foreach (float acc in AccuracyList)
                {
                    oppai_cmd = $"\"{osu_file}\" {acc}% {(string.IsNullOrWhiteSpace(mods_str) ? string.Empty : $"+{mods_str}")} -ojson";

                    var oppai_result = GetOppaiResult(oppai_cmd);

                    p.WaitForExit();
                    p.Close();

                    if (oppai_result != null)
                    {
                        oppai_infos.Add(oppai_result);
                    }
                    else
                    {
                        break;
                    }

                    //add pp
                    OutputDataMap[$"pp:{acc:F2}%"] = oppai_result.pp.ToString();
                    OutputDataMap["mods_str"] = raw_mod_list;

                    IO.CurrentIO.Write($"{acc}  ->  {oppai_result.pp}pp");
                }
            }
            else
            {
                //Listen,so call oppai once only.
                oppai_cmd = $"\"{osu_file}\" -ojson";

                var oppai_result = GetOppaiResult(oppai_cmd);

                if (oppai_result == null)
                {
                    oppai_result = BeatmapReader.GetJsonFromFile(osu_file);
                }

                oppai_infos.Add(oppai_result);
            }
            #region GetBaseInfo

            if (oppai_infos.Count == 0)
            {
                return;
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
            #endregion

            //Fix mod adding.
            OutputDataMap["mods_str"] = raw_mod_list;
            OutputDataMap["mods_full"] = param.Mods;

            //add extra info(shortcut arguments)
            foreach (var pair in extra_data)
            {
                OutputDataMap[pair.Key] = pair.Value;
            }

            AddExtraInfomation(OutputDataMap);

            OutputResult(oppai_infos, OutputDataMap);

            IO.CurrentIO.WriteColor($"[PPCalculator]{PPSHOW_FINISH}{sw.ElapsedMilliseconds}ms", ConsoleColor.Green);
        }

        private void OutputResult(List<OppaiJson> info, Dictionary<string, string> data_dic)
        {
            Utils.RecordTime("OutputResult", () =>
            {
                //info.ForEach(p => data_dic[$"pp:{p.accuracy:F2}%"] = p.pp.ToString());

                RaiseProcessEvent(new PackedMetadataProcessEvent()
                {
                    OutputData = data_dic
                });
            });
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

        private OppaiJson GetOppaiResult(string oppai_cmd)
        {
            oppai_cmd = oppai_cmd.Replace("\r", string.Empty).Replace("\n", string.Empty);

            p.StartInfo.Arguments = oppai_cmd;

            p.Start();

            string output = p.StandardOutput.ReadToEnd();
            string stderr = p.StandardError.ReadToEnd();

            var oppai_result = JsonConvert.DeserializeObject<OppaiJson>(output);

            if (stderr.Length != 0)
            {
                return null;
            }

            return oppai_result;
        }

        private void AddExtraInfomationFromBeatmapFile(string file_path, Dictionary<string, string> extra_data, string mods)
        {
            int status = 0;
            double min_bpm = int.MaxValue, max_bpm = int.MinValue, current_bpm = 0;

            using (StreamReader reader = File.OpenText(file_path))
            {
                //简单的状态机
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    switch (status)
                    {
                        case 0: //seeking
                            if (line == "[Metadata]")
                            {
                                status = 1;
                            }

                            if (line == "[TimingPoints]")
                            {
                                status = 2;
                            }

                            if (line == "[HitObjects]")
                            {
                                return;
                            }
                            break;

                        case 1: //Metadata
                            if (string.IsNullOrWhiteSpace(line))
                            {
                                status = 0;
                                break;
                            }

                            if (line.StartsWith("BeatmapID"))
                            {
                                extra_data["beatmap_id"] = line.Remove(0, 9 + 1).Trim();
                            }
                            if (line.StartsWith("Source"))
                            {
                                extra_data["source"] = line.Remove(0, 6 + 1).Trim();
                            }
                            else if (line.StartsWith("BeatmapSetID"))
                            {
                                extra_data["beatmap_setid"] = line.Remove(0, 12 + 1).Trim();
                            }
                            else if (line.StartsWith("TitleUnicode"))
                            {
                                extra_data["title_unicode"] = line.Remove(0, 12 + 1).Trim();
                            }
                            else if (line.StartsWith("ArtistUnicode"))
                            {
                                extra_data["artist_unicode"] = line.Remove(0, 13 + 1).Trim();
                            }
                            break;

                        case 2: //TimingPoints
                            if (string.IsNullOrWhiteSpace(line))
                            {
                                min_bpm = Math.Round(min_bpm, MidpointRounding.AwayFromZero);
                                max_bpm = Math.Round(max_bpm, MidpointRounding.AwayFromZero);

                                if (mods.Contains("DT") || mods.Contains("NC"))
                                {
                                    min_bpm *= 1.5;
                                    max_bpm *= 1.5;
                                    min_bpm = Math.Round(min_bpm, MidpointRounding.AwayFromZero);
                                    max_bpm = Math.Round(max_bpm, MidpointRounding.AwayFromZero);
                                }
                                if (mods.Contains("HT"))
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
                        default:
                            break;
                    }
                }
            }
        }
    }
}
