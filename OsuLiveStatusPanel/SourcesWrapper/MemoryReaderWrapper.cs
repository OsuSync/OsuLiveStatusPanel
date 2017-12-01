using MemoryReader.BeatmapInfo;
using MemoryReader.Mods;
using NowPlaying;
using Sync.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MemoryReader.Listen.OSUListenerManager;

namespace OsuLiveStatusPanel
{
    class MemoryReaderWrapper
    {
        OsuLiveStatusPanelPlugin RefPlugin;

        public ModsInfo current_mod;

        private int beatmapID, beatmapSetID;

        public string OsuFilePath;

        public MemoryReaderWrapper(OsuLiveStatusPanelPlugin p) => RefPlugin = p;

        public void OnCurrentBeatmapChange(Beatmap beatmap)
        {
            beatmapID = beatmap.BeatmapID;
            OsuFilePath = beatmap.LocationFile;
        }

        public void OnCurrentBeatmapSetChange(BeatmapSet beatmap)
        {
            beatmapSetID = beatmap.BeatmapSetID;
        }

        public void OnCurrentModsChange(ModsInfo mod)
        {
            current_mod = mod;
            IO.CurrentIO.WriteColor($"mod change : {mod.ShortName}", ConsoleColor.Blue);
        }

        public void OnStatusChange(OsuStatus last_status, OsuStatus status)
        {
            if (last_status == status) return;
            if (status != OsuStatus.Playing)
            {
                RefPlugin.OnBeatmapChanged(null);
            }
            else
            {
                //load
                BeatmapEntry beatmap = new BeatmapEntry()
                {
                    BeatmapId = beatmapID,
                    BeatmapSetId = beatmapSetID,
                    OsuFilePath = OsuFilePath
                };

                RefPlugin.OnBeatmapChanged(new BeatmapChangedParameter() { beatmap= beatmap });
            }
        }
    }
}
