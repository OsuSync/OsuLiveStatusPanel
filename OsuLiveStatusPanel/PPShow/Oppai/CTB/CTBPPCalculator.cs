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
        public class CtbCalculateResult
        {
            public CtbServerResult ServerResult { get; set; }
            public double Pp { get; set; }
        }

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
        }

        public void SetMod(Mods.ModsInfo modsInfo)
        {
            var mod = new OsuRTDataProvider.Mods.ModsInfo();
            mod.Mod = (OsuRTDataProvider.Mods.ModsInfo.Mods)((uint)modsInfo.Mod);

            ctb_pp_calc.Mods = mod;
        }

        public double? Calculate(float acc)
        {
            return GetData(acc)?.Pp?? 0;
        }

        private CtbCalculateResult GetData(float acc)
        {
            int retry = 15;

            while (retry != 0)
            {
                try
                {
                    
                    CtbCalculateResult result = new CtbCalculateResult();
                    result.ServerResult = SendGetPp(new ArraySegment<byte>(this.ctb_pp_calc.Beatmap.RawData), ctb_pp_calc.Mods);
                    result.Pp = CalculatePp(result.ServerResult, ctb_pp_calc.Mods, acc, result.ServerResult.FullCombo, 0);
                    return result;
                }
                catch (Exception e)
                {
                    Thread.Sleep(50);
                }
            }

            return null;
        }

        public double? Stars()
        {
            return GetData(100)?.ServerResult.Stars ?? 0;
        }
    }
}
