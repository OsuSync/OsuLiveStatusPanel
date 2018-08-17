using OsuRTDataProvider;
using OsuRTDataProvider.Helper;
using OsuRTDataProvider.Mods;
using static OsuRTDataProvider.Listen.OsuListenerManager;

namespace OsuLiveStatusPanel.SourcesWrapper.ORTDP
{
    /// <summary>
    /// 不支持选图界面获取Mod的版本
    /// </summary>
    internal class OsuRTDataProviderWrapper : RealtimeDataProvideWrapperBase
    {
        public OsuRTDataProviderWrapper(OsuRTDataProviderPlugin ref_plugin, OsuLiveStatusPanelPlugin plugin) : base(ref_plugin, plugin)
        {
        }

        /*
         因为MemoryReader扫Status比Mod快,如果使用Status来做是否开始打图的依据会无法及时获取当前MOD的信息.因此按Mod来判断是否开始打图,null为非打图状态
        */

        public override void OnCurrentModsChange(ModsInfo mod)
        {
            if (current_mod.Mod == mod.Mod) return;

            current_mod = mod;

            if (mod.Mod == ModsInfo.Mods.Unknown)
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

                    beatmap.OutputType = CurrentOutputType = OutputType.Play;

                    RefPanelPlugin.OnBeatmapChanged(new BeatmapChangedParameter() { beatmap = beatmap });
                }
            }
        }

        public override void OnStatusChange(OsuStatus last_status, OsuStatus status)
        {
            current_status = status;

            if (last_status == status) return;
            if ((status != OsuStatus.Playing) && (status != OsuStatus.Rank))
            {
                if (OsuStatusHelper.IsListening(status))
                {
                    TrigListen();
                }
                else
                {
                    RefPanelPlugin.OnBeatmapChanged(null);
                }
            }
            else
            {
                if (current_mod.Mod != ModsInfo.Mods.Unknown || current_mod.Mod != ModsInfo.Mods.None)
                {
                    //fix for https://puu.sh/zelua/d60b98d496.jpg
                    return;
                }

                BeatmapEntry beatmap = new BeatmapEntry()
                {
                    OutputType = CurrentOutputType = OutputType.Listen,
                    BeatmapId = beatmapID,
                    BeatmapSetId = beatmapSetID,
                    OsuFilePath = OsuFilePath,
                    ExtraParam = new System.Collections.Generic.Dictionary<string, object> {
                        { "ortdp_beatmap", current_beatmap },
                        { "mode",RefPlugin.ListenerManager.GetCurrentData(OsuRTDataProvider.Listen.ProvideDataMask.GameMode).PlayMode }
                    }
                };

                RefPanelPlugin.OnBeatmapChanged(new BeatmapChangedParameter() { beatmap = beatmap });
            }
        }
    }
}