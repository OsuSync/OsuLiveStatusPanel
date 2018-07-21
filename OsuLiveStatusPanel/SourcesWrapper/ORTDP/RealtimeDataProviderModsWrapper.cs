using OsuRTDataProvider;
using OsuRTDataProvider.Mods;
using static OsuRTDataProvider.Listen.OsuListenerManager;

namespace OsuLiveStatusPanel.SourcesWrapper.ORTDP
{
    /// <summary>
    /// 支持选图界面获取Mod的版本
    /// </summary>
    internal class RealtimeDataProviderModsWrapper : OsuRTDataProviderWrapper
    {
        public RealtimeDataProviderModsWrapper(OsuRTDataProviderPlugin ref_plugin, OsuLiveStatusPanelPlugin plugin) : base(ref_plugin, plugin)
        {
        }

        public override void OnCurrentModsChange(ModsInfo mod)
        {
            //如果mod相同或者mod是unknown的就不管了
            if (current_mod == mod || mod.Mod.HasFlag(OsuRTDataProvider.Mods.ModsInfo.Mods.Unknown))
                return;
            current_mod = mod;

            //在打图过程中mod变了，那可能就是ortdp要背锅了.jpg
            if (CurrentOutputType == OutputType.Play)
                return;

            //在选图界面改变Mods会输出，会重新计算PP并输出相关信息
            var beatmap = GetCurrentBeatmap();

            beatmap.OutputType = CurrentOutputType;

            RefPanelPlugin.OnBeatmapChanged(new BeatmapChangedParameter() { beatmap = beatmap });
        }

        public override void OnStatusChange(OsuStatus last_status, OsuStatus status)
        {
            current_status = status;

            if (last_status == status) return;
            if ((status != OsuStatus.Playing) && (status != OsuStatus.Rank))
            {
                if (status == OsuStatus.Listening)
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
                BeatmapEntry beatmap = new BeatmapEntry()
                {
                    OutputType = CurrentOutputType = OutputType.Play,
                    BeatmapId = beatmapID,
                    BeatmapSetId = beatmapSetID,
                    OsuFilePath = OsuFilePath,
                    ExtraParam = new System.Collections.Generic.Dictionary<string, object> { { "ortdp_beatmap", current_beatmap } }
                };

                RefPanelPlugin.OnBeatmapChanged(new BeatmapChangedParameter() { beatmap = beatmap });
            }
        }
    }
}