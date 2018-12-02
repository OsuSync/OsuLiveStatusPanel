using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RealTimePPDisplayer.Calculator;
using static RealTimePPDisplayer.Calculator.CatchTheBeatPerformanceCalculator;

namespace OsuLiveStatusPanel.PPShow.BeatmapInfoHanlder
{
    internal class CTBPPCalculator:BeatmapInfoHandlerBase
    {
        private CatchTheBeatPerformanceCalculator ctb_pp_calc;

        public CTBPPCalculator()
        {
            ctb_pp_calc = new CatchTheBeatPerformanceCalculator();
            ctb_pp_calc.Time = int.MaxValue;
        }
        
        public void SetBeatmap(OsuRTDataProvider.BeatmapInfo.Beatmap beatmap)
        {
            if (beatmap == null)
            {
                Log.Error("Can't get OsuRTDataProvider.BeatmapInfo.Beatmap object from SourceWrapper");
                return;
            }

            ctb_pp_calc.Beatmap = new RealTimePPDisplayer.Beatmap.BeatmapReader(beatmap, (int)OsuRTDataProvider.Listen.OsuPlayMode.CatchTheBeat);
        }

        public void SetMod(Mods.ModsInfo modsInfo)
        {
            var mod = new OsuRTDataProvider.Mods.ModsInfo();
            mod.Mod = (OsuRTDataProvider.Mods.ModsInfo.Mods)((uint)modsInfo.Mod);

            ctb_pp_calc.Mods = (uint)mod.Mod;
        }

        enum RequireType
        {
            PP,
            Star,
            AR
        }

        CtbServerResult calc_cache;

        private double? GetData(float acc,RequireType require)
        {
            int retry = 15;

            while (retry != 0)
            {
                try
                {
                    if (calc_cache == null)
                        calc_cache = ctb_pp_calc.SendCalculateCtb(new ArraySegment<byte>(this.ctb_pp_calc.Beatmap.RawData), ctb_pp_calc.Mods);
                    switch (require)
                    {
                        case RequireType.PP:
                            return CalculatePp(calc_cache, ctb_pp_calc.Mods, acc, calc_cache.FullCombo, 0);
                        case RequireType.Star:
                            return calc_cache.Stars;
                        case RequireType.AR:
                            return calc_cache.ApproachRate;
                        default:
                            return null;
                    }
                }
                catch
                {
                    Thread.Sleep(50);
                    retry--;
                }
            }

            return 0;
        }

        public override void HandleExtraData(Dictionary<string, object> extra, Dictionary<string, string> map_info)
        {
            calc_cache = null;
            SetBeatmap(extra["ortdp_beatmap"] as OsuRTDataProvider.BeatmapInfo.Beatmap);
            SetMod((Mods.ModsInfo)extra["Mods"]);

            var list = extra["AccuracyList"] as List<float>;

            foreach (var acc in list)
            {
                var pp=GetData(acc, RequireType.PP);
                map_info[$"pp:{acc:F2}%"] = pp?.ToString("F2")??"0";
            }

            map_info["ar"] = GetData(100, RequireType.AR)?.ToString("F2") ?? map_info["ar"];
            var star=GetData(100, RequireType.Star)?.ToString("F2");

            if (!string.IsNullOrWhiteSpace(star))
                map_info["stars"] = star;
        }
    }
}
