using RealTimePPDisplayer.Calculator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsuLiveStatusPanel.PPShow.Oppai.Mania
{
    class ManiaPPCalculator
    {
        ManiaPerformanceCalculator mania_pp_calc;

        public ManiaPPCalculator()
        {
            mania_pp_calc = new ManiaPerformanceCalculator();
        }

        public void SetBeatmap(OsuRTDataProvider.BeatmapInfo.Beatmap beatmap)
        {
            if (beatmap == null)
            {
                Log.Error("Can't get OsuRTDataProvider.BeatmapInfo.Beatmap object from SourceWrapper");
                return;
            }

            mania_pp_calc.Beatmap = new RealTimePPDisplayer.Beatmap.BeatmapReader(beatmap, OsuRTDataProvider.Listen.OsuPlayMode.Mania);
            mania_pp_calc.Time = int.MaxValue;
            
        }

        public void SetMod(Mods.ModsInfo modsInfo)
        {
            var mod = new OsuRTDataProvider.Mods.ModsInfo();
            mod.Mod = (OsuRTDataProvider.Mods.ModsInfo.Mods)((uint)modsInfo.Mod);

            mania_pp_calc.Mods = mod;
        }

        public double? Calculate(float acc)
        {
            if (mania_pp_calc == null)
                return null;

            mania_pp_calc.Time = int.MaxValue;

            mania_pp_calc.Score = (int)(1000000*acc/100.0f);

            return mania_pp_calc.GetPP().RealTimePP;
        }
    }
}
