using OsuRTDataProvider.BeatmapInfo;
using OsuRTDataProvider.Mods;
using NowPlaying;
using OsuRTDataProvider.BeatmapInfo;
using Sync.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OsuRTDataProvider.Listen.OsuListenerManager;
using OsuRTDataProvider;

namespace OsuLiveStatusPanel
{
    public class OsuRTDataProviderWrapper:SourceWrapperBase<OsuRTDataProviderPlugin>
    {
        public ModsInfo current_mod;

        private int beatmapID, beatmapSetID;

        private OsuStatus current_status;

        public string OsuFilePath;

        private bool trig = false;

        public OsuRTDataProviderWrapper(OsuRTDataProviderPlugin ref_plugin, OsuLiveStatusPanelPlugin plugin) : base(ref_plugin, plugin)
        {

        }

        public void OnCurrentBeatmapChange(Beatmap beatmap)
        {
            beatmapID = beatmap.BeatmapID;
            beatmapSetID = beatmap.BeatmapSetID;
            OsuFilePath = beatmap.FilenameFull;
        }

        /*
         因为MemoryReader扫Status比Mod快,如果使用Status来做是否开始打图的依据会无法及时获取当前MOD的信息.因此按Mod来判断是否开始打图,null为非打图状态
        */
        
        public void OnCurrentModsChange(ModsInfo mod)
        {
            if (current_mod.Mod == mod.Mod) return;

            current_mod = mod;

            if (mod.Mod==ModsInfo.Mods.Unknown)
            {
                //Not Playing
                //RefPlugin.OnBeatmapChanged(null);
            }
            else
            {
                //Start to play
                //if (mod.Mod!=ModsInfo.Mods.None&&current_status == OsuStatus.Playing)
                {
                    BeatmapEntry beatmap = new BeatmapEntry()
                    {
                        BeatmapId = beatmapID,
                        BeatmapSetId = beatmapSetID,
                        OsuFilePath = OsuFilePath
                    };

                    RefPanelPlugin.OnBeatmapChanged(this,new BeatmapChangedParameter() { beatmap = beatmap });
                }
            }
        }

        public void OnStatusChange(OsuStatus last_status, OsuStatus status)
        {
            current_status = status;
            
            if (last_status == status) return;
            if ((status != OsuStatus.Playing) && (status != OsuStatus.Rank))
            {
                RefPanelPlugin.OnBeatmapChanged(this,null);
            }
            else
            {

            }
        }

        public override void Detach()
        {
            RefPlugin.ListenerManager.OnBeatmapChanged -= OnCurrentBeatmapChange;
            RefPlugin.ListenerManager.OnStatusChanged -= OnStatusChange;
            RefPlugin.ListenerManager.OnModsChanged -= OnCurrentModsChange;
        }

        public override bool Attach()
        {
            RefPlugin.ListenerManager.OnBeatmapChanged += OnCurrentBeatmapChange;
            RefPlugin.ListenerManager.OnStatusChanged += OnStatusChange;
            RefPlugin.ListenerManager.OnModsChanged += OnCurrentModsChange;

            return true;
        }
    }
}
