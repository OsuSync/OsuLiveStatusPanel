using OsuRTDataProvider;
using OsuRTDataProvider.BeatmapInfo;
using OsuRTDataProvider.Mods;
using static OsuRTDataProvider.Listen.OsuListenerManager;

namespace OsuLiveStatusPanel.SourcesWrapper.ORTDP
{
    internal abstract class RealtimeDataProvideWrapperBase : SourceWrapperBase<OsuRTDataProviderPlugin>
    {
        public ModsInfo current_mod;

        protected int beatmapID, beatmapSetID;

        protected OsuStatus current_status;

        public string OsuFilePath;

        public RealtimeDataProvideWrapperBase(OsuRTDataProviderPlugin ref_plugin, OsuLiveStatusPanelPlugin plugin) : base(ref_plugin, plugin)
        {
            RefPanelPlugin.OnSettingChanged += () =>
            {
                var beatmap = GetCurrentBeatmap();

                beatmap.OutputType = CurrentOutputType = (current_status == OsuStatus.Playing || current_status == OsuStatus.Rank) ? OutputType.Play : OutputType.Listen;

                RefPanelPlugin.OnBeatmapChanged(new BeatmapChangedParameter() { beatmap = beatmap });
            };
        }

        public void OnCurrentBeatmapChange(Beatmap beatmap)
        {
            if (beatmap == Beatmap.Empty || string.IsNullOrWhiteSpace(beatmap?.FilenameFull))
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

        public abstract void OnCurrentModsChange(ModsInfo mod);

        protected BeatmapEntry GetCurrentBeatmap()
        {
            return new BeatmapEntry()
            {
                BeatmapId = beatmapID,
                BeatmapSetId = beatmapSetID,
                OsuFilePath = OsuFilePath
            };
        }

        public abstract void OnStatusChange(OsuStatus last_status, OsuStatus status);

        protected void TrigListen()
        {
            var beatmap = GetCurrentBeatmap();

            beatmap.OutputType = CurrentOutputType = OutputType.Listen;

            RefPanelPlugin.OnBeatmapChanged(new BeatmapChangedParameter() { beatmap = beatmap });
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