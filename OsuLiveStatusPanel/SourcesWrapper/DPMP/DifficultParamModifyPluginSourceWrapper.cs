using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DifficultParamModifyPlugin;

namespace OsuLiveStatusPanel.SourcesWrapper.DPMP
{
    class DifficultParamModifyPluginSourceWrapper : SourceWrapperBase<DifficultParamModifyPlugin.DifficultParamModifyPlugin>
    {
        public Mods.ModsInfo CurrentMod
        {
            get
            {
                return new Mods.ModsInfo() {
                Mod= (Mods.ModsInfo.Mods)((uint)RefPlugin.CurrentMods.Mod)
                };
            }
        }

        public DifficultParamModifyPluginSourceWrapper(DifficultParamModifyPlugin.DifficultParamModifyPlugin ref_plugin, OsuLiveStatusPanelPlugin plugin) : base(ref_plugin, plugin)
        {

        }

        private void RefPlugin_OnBeatmapChanged(string osu_file, int set_id, int id, bool output_type)
        {
            RefPanelPlugin.OnBeatmapChanged(this, new BeatmapChangedParameter() {
                beatmap = new BeatmapEntry()
                {
                    BeatmapId = id,
                    BeatmapSetId = set_id,
                    OsuFilePath=osu_file,
                    OutputType=output_type?OutputType.Play:OutputType.Listen
                }
            });
        }

        public override bool Attach()
        {
            this.RefPlugin.OnBeatmapChanged += RefPlugin_OnBeatmapChanged;
            return true;
        }

        public override void Detach()
        {
            this.RefPlugin.OnBeatmapChanged -= RefPlugin_OnBeatmapChanged;
        }
    }
}
