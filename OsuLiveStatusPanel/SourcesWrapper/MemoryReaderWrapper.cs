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

        /*
         因为MemoryReader扫Status比Mod快,如果使用Status来做是否开始打图的依据会无法及时获取当前MOD的信息.因此按Mod来判断是否开始打图,null为非打图状态
        */
        
        public void OnCurrentModsChange(ModsInfo mod)
        {
            if (current_mod == mod) return;

            current_mod = mod;

            if (mod==null)
            {
                //Not Playing
                RefPlugin.OnBeatmapChanged(null);
            }
            else
            {
                //Start to play
                BeatmapEntry beatmap = new BeatmapEntry()
                {
                    BeatmapId = beatmapID,
                    BeatmapSetId = beatmapSetID,
                    OsuFilePath = OsuFilePath
                };

                RefPlugin.OnBeatmapChanged(new BeatmapChangedParameter() { beatmap = beatmap });
            }
        }

        public void OnStatusChange(OsuStatus last_status, OsuStatus status)
        {
            /*
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
            */
        }
    }
}
