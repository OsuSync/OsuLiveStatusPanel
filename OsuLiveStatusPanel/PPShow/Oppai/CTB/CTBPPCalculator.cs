using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RealTimePPDisplayer.Calculator;
using static RealTimePPDisplayer.Calculator.CatchTheBeatPerformanceCalculator;

namespace OsuLiveStatusPanel.PPShow.Oppai.CTB
{
    internal class CTBPPCalculator
    {
        private CatchTheBeatPerformanceCalculator ctb_pp_calc;

        public CTBPPCalculator()
        {
            ctb_pp_calc = new CatchTheBeatPerformanceCalculator();
        }
        
        public void SetBeatmap(OsuRTDataProvider.BeatmapInfo.Beatmap beatmap)
        {
            if (beatmap == null)
            {
                Log.Error("Can't get OsuRTDataProvider.BeatmapInfo.Beatmap object from SourceWrapper");
                return;
            }

            ctb_pp_calc.Beatmap = new RealTimePPDisplayer.Beatmap.BeatmapReader(beatmap, OsuRTDataProvider.Listen.OsuPlayMode.Mania);
            ctb_pp_calc.Time = int.MaxValue;
            calc_cache = null;
        }

        public void SetMod(Mods.ModsInfo modsInfo)
        {
            var mod = new OsuRTDataProvider.Mods.ModsInfo();
            mod.Mod = (OsuRTDataProvider.Mods.ModsInfo.Mods)((uint)modsInfo.Mod);

            ctb_pp_calc.Mods = mod;
            calc_cache = null;
        }

        public double? Calculate(float acc)
        {
            return GetData(acc,RequireType.PP);
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
                    switch (require)
                    {
                        case RequireType.PP:
                            if (calc_cache == null)
                                calc_cache = SendGetPp(new ArraySegment<byte>(this.ctb_pp_calc.Beatmap.RawData), ctb_pp_calc.Mods);
                            return CalculatePp(calc_cache, ctb_pp_calc.Mods, acc, calc_cache.FullCombo, 0);
                        case RequireType.Star:
                            return SendGetPp(new ArraySegment<byte>(this.ctb_pp_calc.Beatmap.RawData), ctb_pp_calc.Mods).Stars;
                        case RequireType.AR:
                            return SendGetPp(new ArraySegment<byte>(this.ctb_pp_calc.Beatmap.RawData), ctb_pp_calc.Mods).ApproachRate;
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

        public double? Stars()
        {
            return GetData(100,RequireType.Star);
        }
    }
}
