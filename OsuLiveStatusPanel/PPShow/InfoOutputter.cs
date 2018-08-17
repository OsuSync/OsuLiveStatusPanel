using Newtonsoft.Json;
using OsuLiveStatusPanel.Mods;
using OsuLiveStatusPanel.PPShow.Beatmap;
using OsuLiveStatusPanel.PPShow.BeatmapInfoHanlder;
using OsuLiveStatusPanel.PPShow.BeatmapInfoHanlder.Oppai;
using OsuLiveStatusPanel.PPShow.Oppai;
using Sync.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace OsuLiveStatusPanel.PPShow
{
    internal class InfoOutputter
    {
        private static readonly ModsInfo.Mods[] OPPAI_SUPPORT_MODS = new[] { ModsInfo.Mods.NoFail, ModsInfo.Mods.Easy, ModsInfo.Mods.Hidden, ModsInfo.Mods.HardRock, ModsInfo.Mods.DoubleTime, ModsInfo.Mods.HalfTime, ModsInfo.Mods.Nightcore, ModsInfo.Mods.Flashlight, ModsInfo.Mods.SpunOut };

        private BeatmapInfoHandlerBase[] ModeHandler = new BeatmapInfoHandlerBase[4]; 

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

            ModeHandler[0] = ModeHandler[1] = new OppaiHandlerBase();

            if (CheckExsitRealtimePPPlugin())
            {
                ModeHandler[2] = new CTBPPCalculator();
                Log.Output("find RealtimePP plugin,OLSP is support ctb pp calculating as well.");

                ModeHandler[3] = new ManiaPPCalculator();
                Log.Output("find RealtimePP plugin,OLSP is support mania pp calculating as well.");
            }
        }

        private bool CheckExsitRealtimePPPlugin() => Sync.SyncHost.Instance.EnumPluings().Any(plugin => plugin.Name == "RealTimePPDisplayer");

        private string OpenReadBeatmapParamValue(ref byte[] beatmap_raw_data, string Name)
        {
            using (var reader = new StreamReader(new MemoryStream(beatmap_raw_data)))
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();

                    if (line.StartsWith(Name))
                        return line.Remove(0, Name.Length + 1).Trim();
                }
            
            return null;
        }

        public bool TrigOutput(OutputType output_type, string osu_file_path, ModsInfo mods, Dictionary<string, object> extra)
        {
            List<OppaiJson> oppai_infos = new List<OppaiJson>();

            Dictionary<string, string> OutputDataMap = new Dictionary<string, string>();

            if (extra != null)
                foreach (var data in extra)
                    OutputDataMap[data.Key] = data.Value.ToString();

            if (!string.IsNullOrWhiteSpace(mods.ShortName))
            {
                mods = FilteVailedMod(mods);
            }

            OutputDataMap["mods_str"] = mods.ShortName;

            extra["AccuracyList"] = AccuracyList;
            extra["Mods"] = mods;

            var stream = File.ReadAllBytes(osu_file_path);

            var mode = int.Parse(OpenReadBeatmapParamValue(ref stream, "Mode") ?? "0");

            if (ModeHandler[mode] != null)
            {
                var handler = ModeHandler[mode];

                handler.HandleBaseInfo(OutputDataMap, ref stream, extra);

                handler.HandleExtraData(extra, OutputDataMap);

                handler.AddExtraBeatmapInfo(OutputDataMap);
            }



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
    }
}