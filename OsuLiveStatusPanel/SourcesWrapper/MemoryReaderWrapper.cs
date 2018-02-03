using OsuRTDataProvider.BeatmapInfo;
using OsuRTDataProvider.Mods;
using NowPlaying;
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
    internal class OsuRTDataProviderWrapper:SourceWrapperBase<OsuRTDataProviderPlugin>
    {
        public ModsInfo current_mod;

        private int beatmapID, beatmapSetID;

        private OsuStatus current_status;

        public string OsuFilePath;

        public OsuRTDataProviderWrapper(OsuRTDataProviderPlugin ref_plugin, OsuLiveStatusPanelPlugin plugin) : base(ref_plugin, plugin)
        {
            RefPanelPlugin.OnSettingChanged += () =>
            {
                var beatmap = GetCurrentBeatmap();

                if (current_status == OsuStatus.Playing || current_status == OsuStatus.Rank)
                    beatmap.OutputType = OutputType.Play;
                else
                    beatmap.OutputType = OutputType.Listen;

                RefPanelPlugin.OnBeatmapChanged(this, new BeatmapChangedParameter() { beatmap =  beatmap});
            };
        }

        public void OnCurrentBeatmapChange(Beatmap beatmap)
        {
            if (beatmap==Beatmap.Empty||string.IsNullOrWhiteSpace(beatmap?.FilenameFull))
            {
                //fix empty beatmap
                return;
            }

            beatmapID = beatmap.BeatmapID;
            beatmapSetID = beatmap.BeatmapSetID;
            OsuFilePath = beatmap.FilenameFull;

            if (current_status == OsuStatus.Listening)
            {
                TrigListen();
            }
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
                    var beatmap = GetCurrentBeatmap();

                    beatmap.OutputType = OutputType.Play;

                    RefPanelPlugin.OnBeatmapChanged(this,new BeatmapChangedParameter() { beatmap = beatmap });
                }
            }
        }

        private BeatmapEntry GetCurrentBeatmap()
        {
            return new BeatmapEntry()
            {
                BeatmapId = beatmapID,
                BeatmapSetId = beatmapSetID,
                OsuFilePath = OsuFilePath
            };
        }

        public void OnStatusChange(OsuStatus last_status, OsuStatus status)
        {
            current_status = status;
            
            if (last_status == status) return;
            if ((status != OsuStatus.Playing) && (status != OsuStatus.Rank))
            {
                if (status==OsuStatus.Listening)
                {
                    TrigListen();
                }
                else
                {
                    RefPanelPlugin.OnBeatmapChanged(this, null);
                }
            }
            else
            {
                if (current_mod.Mod!=ModsInfo.Mods.Unknown||current_mod.Mod!=ModsInfo.Mods.None)
                {
                    //fix for https://puu.sh/zelua/d60b98d496.jpg
                    return;
                }

                BeatmapEntry beatmap = new BeatmapEntry()
                {
                    OutputType=OutputType.Listen,
                    BeatmapId = beatmapID,
                    BeatmapSetId = beatmapSetID,
                    OsuFilePath = OsuFilePath
                };

                RefPanelPlugin.OnBeatmapChanged(this, new BeatmapChangedParameter() { beatmap = beatmap });
            }
        }

        private void TrigListen()
        {
            var beatmap = GetCurrentBeatmap();

            beatmap.OutputType = OutputType.Listen;

            RefPanelPlugin.OnBeatmapChanged(this, new BeatmapChangedParameter() { beatmap = beatmap });
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
