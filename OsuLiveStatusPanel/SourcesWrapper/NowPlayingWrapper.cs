using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NowPlaying;
using OsuLiveStatusPanel.ProcessEvent;
using Sync.Plugins;
using Sync.Tools;

namespace OsuLiveStatusPanel
{
    internal class NowPlayingWrapper : SourceWrapperBase<NowPlaying.NowPlaying>
    {
        private NowPlaying.BeatmapEntry current_beatmap;

        class ModChangedRecevicer : ProcessEvent.ProcessRecevierBase
        {
            public override void OnEventRegister(BaseEventDispatcher<IPluginEvent> EventBus)
            {
                EventBus.BindEvent<ProcessEvent.BeatmapChangedProcessEvent>(e => this.RaiseProcessEvent(new ProcessEvent.MetadataProcessEvent() {
                    Name="mods",
                    Value="None"
                }));
            }
        }

        public NowPlayingWrapper(NowPlaying.NowPlaying ref_plugin, OsuLiveStatusPanelPlugin plugin) : base(ref_plugin, plugin)
        {
            RefPanelPlugin.RegisterProcess(new ModChangedRecevicer());
            RefPanelPlugin.OnSettingChanged += () =>
            {
                RefPanelPlugin.RaiseProcessEvent(new StatusWrapperProcessEvent() {
                    Beatmap = current_beatmap == null ? null : new BeatmapEntry()
                    {
                        OutputType = CurrentOutputType = OutputType.Play,
                        OsuFilePath = current_beatmap.OsuFilePath,
                        BeatmapId = current_beatmap.BeatmapId,
                        BeatmapSetId = current_beatmap.BeatmapSetId,
                        Artist = current_beatmap.AvailableArtist,
                        Title = current_beatmap.AvailableTitle,
                    },

                    Mods="None",
                    ShortMods=""
                });
            };
        }

        public override bool Attach()
        {
            NowPlayingEvents.Instance.BindEvent<CurrentPlayingBeatmapChangedEvent>((beatmap) => {
                
                current_beatmap = beatmap.NewBeatmap;

                if (beatmap.NewBeatmap == null)
                {
                    RefPanelPlugin.RaiseProcessEvent(new ClearProcessEvent());
                }
                else
                {
                    RefPanelPlugin.RaiseProcessEvent(new StatusWrapperProcessEvent()
                    {
                        Beatmap = new BeatmapEntry()
                        {
                            OutputType = CurrentOutputType = OutputType.Play,
                            OsuFilePath = current_beatmap.OsuFilePath,
                            BeatmapId = current_beatmap.BeatmapId,
                            BeatmapSetId = current_beatmap.BeatmapSetId,
                            Artist = current_beatmap.AvailableArtist,
                            Title = current_beatmap.AvailableTitle,
                        },

                        Mods = "None",
                        ShortMods = ""
                    });
                }

                //RefPanelPlugin.RaiseProcessEvent(new StatusChangeProcessEvent() { OutputType = current_beatmap==null?OutputType.Listen: OutputType.Play });
            });

            return true;
        }

        public override void Detach()
        {
            IO.CurrentIO.WriteColor("NowPlaying not implement remove event!",ConsoleColor.Red);
        }
    }
}
