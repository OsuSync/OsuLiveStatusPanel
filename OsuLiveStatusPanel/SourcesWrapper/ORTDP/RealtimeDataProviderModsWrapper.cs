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
            if (current_mod == mod)
                return;
            current_mod = mod;

            if (CurrentOutputType == OutputType.Play)
                return;

            //选图界面改变Mods会输出

            var beatmap = GetCurrentBeatmap();

            beatmap.OutputType = CurrentOutputType;

            RefPanelPlugin.OnBeatmapChanged(this, new BeatmapChangedParameter() { beatmap = beatmap });
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
                    RefPanelPlugin.OnBeatmapChanged(this, null);
                }
            }
            else
            {
                BeatmapEntry beatmap = new BeatmapEntry()
                {
                    OutputType = CurrentOutputType = OutputType.Play,
                    BeatmapId = beatmapID,
                    BeatmapSetId = beatmapSetID,
                    OsuFilePath = OsuFilePath
                };

                RefPanelPlugin.OnBeatmapChanged(this, new BeatmapChangedParameter() { beatmap = beatmap });
            }
        }
    }
}