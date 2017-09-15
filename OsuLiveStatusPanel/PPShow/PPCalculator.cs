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
        
        public void TrigCalc(string osu_file_path,string mods_list)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

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
            OnOppainJson?.Invoke(oppai_infos);

            IO.CurrentIO.WriteColor($"[PPCalculator]执行结束,用时 {sw.ElapsedMilliseconds}ms",ConsoleColor.Green);
            sw.Stop();
        }
    }
}
