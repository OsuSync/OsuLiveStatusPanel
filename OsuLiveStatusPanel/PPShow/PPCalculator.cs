using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Sync.Tools;

namespace OsuLiveStatusPanel
{
    class PPCalculator
    {
        public List<float> AccuracyList;
        public delegate void OnBeatmapChangedEvt(List<OppaiJson> info);
        public event OnBeatmapChangedEvt OnOppainJson;

        public delegate void OnBackMenuEvt();
        public event OnBackMenuEvt OnBackMenu;

        Process p=null;

        string oppai;

        public PPCalculator(string oppai,List<float> acc_list)
        {
            AccuracyList = acc_list;

            this.oppai = oppai;
        }
        
        public void TrigCalc(string osu_file_path,string mods_list,KeyValuePair<string,string>[] extra=null)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            Dictionary<string, string> extra_data = new Dictionary<string, string>();

            if (p == null)
            {
                p = new Process();
                p.StartInfo.FileName = oppai;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.CreateNoWindow = true;
            }
            
            if (string.IsNullOrWhiteSpace(osu_file_path))
            {
                OnBackMenu?.Invoke();
                return;
            }


            if (extra != null)
            {
                foreach (var data in extra)
                {
                    extra_data[data.Key] = data.Value;
                }
            }

            AddData(osu_file_path, extra_data);
            
            string osu_file = osu_file_path;
            string mods_str = mods_list;

            if (mods_str == "None") mods_str = "";

            List<OppaiJson> oppai_infos = new List<OppaiJson>();

            foreach (float acc in AccuracyList)
            {
                string oppai_cmd = $"\"{osu_file}\" {acc}% -ojson";
                if (mods_str.Length != 0)
                    oppai_cmd = $"\"{osu_file}\" {acc}% +{mods_str} -ojson";

                oppai_cmd = oppai_cmd.Replace("\r", string.Empty).Replace("\n", string.Empty);

                p.StartInfo.Arguments = oppai_cmd;

                p.Start();

                p.StandardInput.AutoFlush = true;

                string output = p.StandardOutput.ReadToEnd();
                string stderr = p.StandardError.ReadToEnd();
                if (stderr.Length != 0)
                {
                    IO.CurrentIO.WriteColor("[PPCalculator]Beatmap无法打开或解析", ConsoleColor.Red);
                    return;
                }

                var oppai_json = JsonConvert.DeserializeObject<OppaiJson>(output);
                oppai_json.accuracy = acc;
                oppai_json.filepath = osu_file;

                oppai_infos.Add(oppai_json);

                p.WaitForExit();
                p.Close();
            }

            oppai_infos.AsParallel().ForAll((oppai_json) => 
            {
                foreach (var pair in extra_data)
                {
                    switch (pair.Key)
                    {
                        case "beatmap_id":
                            oppai_json.beatmap_id = int.Parse(pair.Value);
                            break;
                        case "source":
                            oppai_json.source = pair.Value;
                            break;
                        case "beatmap_setid":
                            oppai_json.beatmap_id = int.Parse(pair.Value);
                            break;
                        case "title_unicode":
                            oppai_json.title_unicode = pair.Value;
                            break;
                        case "artist_unicode":
                            oppai_json.artist_unicode = pair.Value;
                            break;
                        case "min_bpm":
                            oppai_json.min_bpm = float.Parse(pair.Value);
                            break;
                        case "max_bpm":
                            oppai_json.max_bpm = float.Parse(pair.Value);
                            break;
                        default:
                            break;
                    }
                }

            });

            OnOppainJson?.Invoke(oppai_infos);

            IO.CurrentIO.WriteColor($"[PPCalculator]执行结束,用时 {sw.ElapsedMilliseconds}ms",ConsoleColor.Green);
            sw.Stop();
        }

        private void AddData(string file_path,Dictionary<string,string> extra_data)
        {
            int status = 0;
            float min_bpm = int.MaxValue,max_bpm = int.MinValue,current_bpm =0;

            using (StreamReader reader = File.OpenText(file_path))
            {
                //简单的状态机
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    switch (status)
                    {
                        case 0: //seeking
                            if (line== "[Metadata]")
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
                                extra_data["source"] = line.Remove(0, 9 + 1).Trim();
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

                                max_bpm /= 2;//我也不知道为啥要加这个

#if DEBUG
                                IO.CurrentIO.Write($"[Oppai]BPM:{min_bpm} ~ {max_bpm}");
#endif

                                extra_data["min_bpm"] = min_bpm.ToString();
                                extra_data["max_bpm"] = max_bpm.ToString();

                                status = 0;
                                break;
                            }

                            string[] data = line.Split(',');
                            if (data.Length<8)
                            {
                                break;
                            }

                            float val = float.Parse(data[1]);

                            if (val>0)
                            {
                                val = 60000 / val;
                                current_bpm = val;
                            }
                            else
                            {
                                float mul = Math.Abs(100 + val)/100.0f;
                                val = current_bpm * (1 + mul);
                            }

                            val = (float)Math.Round(val);

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
