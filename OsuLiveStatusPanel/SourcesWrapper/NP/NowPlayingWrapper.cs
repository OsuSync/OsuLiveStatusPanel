using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NowPlaying;
using Sync.Tools;

namespace OsuLiveStatusPanel
{
    internal class NowPlayingWrapper : SourceWrapperBase<NowPlaying.NowPlaying>
    {
        private NowPlaying.BeatmapEntry current_beatmap;

        public NowPlayingWrapper(NowPlaying.NowPlaying ref_plugin, OsuLiveStatusPanelPlugin plugin) : base(ref_plugin, plugin)
        {
            RefPanelPlugin.OnSettingChanged += () =>
            {
                RefPanelPlugin.OnBeatmapChanged(this, new BeatmapChangedParameter()
                {
                    beatmap = current_beatmap == null ? null : new BeatmapEntry()
                    {
                        OutputType = CurrentOutputType = OutputType.Play,
                        OsuFilePath = current_beatmap.OsuFilePath,
                        BeatmapId = current_beatmap.BeatmapId,
                        BeatmapSetId = current_beatmap.BeatmapSetId
                    }
                });
            };
        }

        public override bool Attach()
        {
            NowPlayingEvents.Instance.BindEvent<CurrentPlayingBeatmapChangedEvent>((beatmap) => {
                RefPanelPlugin.OnBeatmapChanged(this,new BeatmapChangedParameter()
                {
                    beatmap = beatmap.NewBeatmap == null ? null : new BeatmapEntry()
                    {
                        OutputType= CurrentOutputType = OutputType.Play,
                        OsuFilePath = beatmap.NewBeatmap.OsuFilePath,
                        BeatmapId = beatmap.NewBeatmap.BeatmapId,
                        BeatmapSetId = beatmap.NewBeatmap.BeatmapSetId
                    }
                });
                current_beatmap = beatmap.NewBeatmap;
            });

            return true;
        }

        public override void Detach()
        {
            IO.CurrentIO.WriteColor("NowPlaying not implement remove event!",ConsoleColor.Red);
        }
    }
}
