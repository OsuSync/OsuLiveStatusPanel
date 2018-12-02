using System.Collections.Generic;
using RealTimePPDisplayer.Calculator;

namespace OsuLiveStatusPanel.PPShow.BeatmapInfoHanlder
{
    internal class ManiaPPCalculator: BeatmapInfoHandlerBase
    {
        private ManiaPerformanceCalculator mania_pp_calc;

        public ManiaPPCalculator()
        {
            mania_pp_calc = new ManiaPerformanceCalculator();
        }

        public override void HandleExtraData(Dictionary<string, object> extra, Dictionary<string, string> map_info)
        {
            var beatmap = extra["ortdp_beatmap"] as OsuRTDataProvider.BeatmapInfo.Beatmap;
            mania_pp_calc.Beatmap = new RealTimePPDisplayer.Beatmap.BeatmapReader(beatmap, (int)OsuRTDataProvider.Listen.OsuPlayMode.Mania);
            mania_pp_calc.Time = int.MaxValue;

            var mod = new OsuRTDataProvider.Mods.ModsInfo();
            mod.Mod = (OsuRTDataProvider.Mods.ModsInfo.Mods)((uint)((Mods.ModsInfo)extra["Mods"]).Mod);

            mania_pp_calc.Mods = (uint)mod.Mod;

            mania_pp_calc.Time = int.MaxValue;

            var list = extra["AccuracyList"] as List<float>;

            foreach (var acc in list)
            {
                mania_pp_calc.Score = (int)(1000000 * acc / 100.0f);

                var pp= mania_pp_calc.GetPerformance().RealTimePP;

                map_info[$"pp:{acc:F2}%"] = pp.ToString("F2");
            }
        }
    }
}