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
using OsuLiveStatusPanel.ProcessEvent;

namespace OsuLiveStatusPanel
{
    internal class OsuRTDataProviderWrapper:SourceWrapperBase<OsuRTDataProviderPlugin>
    {
        public ModsInfo current_mod;

        private OsuStatus current_status;

        public Beatmap current_beatmap;

        public OsuRTDataProviderWrapper(OsuRTDataProviderPlugin ref_plugin, OsuLiveStatusPanelPlugin plugin) : base(ref_plugin, plugin)
        {
            RefPanelPlugin.OnSettingChanged += () =>
            {
                var beatmap = GetCurrentBeatmap();

                beatmap.OutputType = CurrentOutputType = (current_status == OsuStatus.Playing || current_status == OsuStatus.Rank) ? OutputType.Play : OutputType.Listen;

                RefPanelPlugin.RaiseProcessEvent(new StatusWrapperProcessEvent() {
                    Beatmap = beatmap,
                    Mods = current_mod.Name,
                    ShortMods = current_mod.ShortName
                });
            };
        }

        public void OnCurrentBeatmapChange(Beatmap beatmap)
        {
            if (beatmap==Beatmap.Empty||string.IsNullOrWhiteSpace(beatmap?.FilenameFull))
            {
                //fix empty beatmap
                return;
            }

            current_beatmap = beatmap;

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

                    beatmap.OutputType = CurrentOutputType = OutputType.Play;

                    RefPanelPlugin.RaiseProcessEvent(new StatusWrapperProcessEvent()
                    {
                        Beatmap = beatmap,
                        Mods = current_mod.Name,
                        ShortMods = current_mod.ShortName
                    });
                }
            }
        }

        private BeatmapEntry GetCurrentBeatmap()
        {
            return new BeatmapEntry()
            {
                BeatmapId = current_beatmap.BeatmapID,
                BeatmapSetId = current_beatmap.BeatmapSetID,
                OsuFilePath = current_beatmap.FilenameFull,
                Artist = string.IsNullOrWhiteSpace(current_beatmap.ArtistUnicode) ? current_beatmap.Artist : current_beatmap.ArtistUnicode,
                Title = string.IsNullOrWhiteSpace(current_beatmap.TitleUnicode) ? current_beatmap.Title : current_beatmap.TitleUnicode,
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
                    //RefPanelPlugin.RaiseProcessEvent(new ProcessEvent.StatusChangeProcessEvent() { OutputType = OutputType.Listen });
                    TrigListen();
                }
                else
                {
                    RefPanelPlugin.RaiseProcessEvent(new ClearProcessEvent());
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
                    OutputType= CurrentOutputType = OutputType.Listen,
                    BeatmapId = current_beatmap.BeatmapID,
                    BeatmapSetId = current_beatmap.BeatmapSetID,
                    OsuFilePath = current_beatmap.FilenameFull,
                    Artist = string.IsNullOrWhiteSpace(current_beatmap.ArtistUnicode)? current_beatmap.Artist: current_beatmap.ArtistUnicode,
                    Title = string.IsNullOrWhiteSpace(current_beatmap.TitleUnicode) ? current_beatmap.Title : current_beatmap.TitleUnicode,
                };


                //RefPanelPlugin.OnBeatmapChanged(new BeatmapChangedParameter() { beatmap = beatmap });
                RefPanelPlugin.RaiseProcessEvent(new StatusWrapperProcessEvent()
                {
                    Beatmap = beatmap,
                    Mods = current_mod.Name,
                    ShortMods = current_mod.ShortName
                });
            }
        }

        private void TrigListen()
        {
            var beatmap = GetCurrentBeatmap();

            beatmap.OutputType = CurrentOutputType  = OutputType.Listen;

            //RefPanelPlugin.OnBeatmapChanged(new BeatmapChangedParameter() { beatmap = beatmap });
            RefPanelPlugin.RaiseProcessEvent(new StatusWrapperProcessEvent()
            {
                Beatmap = beatmap,
                Mods = current_mod.Name,
                ShortMods = current_mod.ShortName
            });
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
