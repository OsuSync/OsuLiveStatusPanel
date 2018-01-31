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
        public NowPlayingWrapper(NowPlaying.NowPlaying ref_plugin, OsuLiveStatusPanelPlugin plugin) : base(ref_plugin, plugin)
        {
        }

        public override bool Attach()
        {
            NowPlayingEvents.Instance.BindEvent<CurrentPlayingBeatmapChangedEvent>((beatmap) => {
                RefPanelPlugin.OnBeatmapChanged(this,new BeatmapChangedParameter()
                {
                    beatmap = beatmap.NewBeatmap == null ? null : new BeatmapEntry()
                    {
                        OutputType=OutputType.Play,
                        OsuFilePath = beatmap.NewBeatmap.OsuFilePath,
                        BeatmapId = beatmap.NewBeatmap.BeatmapId,
                        BeatmapSetId = beatmap.NewBeatmap.BeatmapSetId
                    }
                });
            });

            return true;
        }

        public override void Detach()
        {
            IO.CurrentIO.WriteColor("NowPlaying not implement remove event!",ConsoleColor.Red);
        }
    }
}
