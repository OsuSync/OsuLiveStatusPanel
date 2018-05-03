using NowPlaying;
using Sync;
using Sync.Plugins;
using Sync.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Effects;
using static OsuRTDataProvider.Listen.OsuListenerManager;
using static OsuLiveStatusPanel.Languages;
using System.Numerics;
using System.Reflection;
using OsuLiveStatusPanel.ProcessEvent;

namespace OsuLiveStatusPanel
{
    [SyncPluginID("dcca15cb-8b8c-4375-934c-2c2b34862e33","1.1.5")]
    public class OsuLiveStatusPanelPlugin : Plugin, IConfigurable
    {
        private enum UsingSource
        {
            OsuRTDataProvider,
            NowPlaying,
            None
        }

        SourceWrapperBase SourceWrapper;

        #region Options

        public ConfigurationElement AllowUsedOsuRTDataProvider { get; set; } = "0";
        public ConfigurationElement AllowUsedNowPlaying { get; set; } = "1";

        public ConfigurationElement Width { get; set; } = "1920";
        public ConfigurationElement Height { get; set; } = "1080";

        public ConfigurationElement EnableOutputModPicture { get; set; } = "0";
        public ConfigurationElement OutputModImageFilePath { get; set; } = @"..\output_mod.png";

        public ConfigurationElement ModUnitPixel { get; set; } = "90";
        public ConfigurationElement ModUnitOffset { get; set; } = "10";

        public ConfigurationElement ModSortReverse { get; set; } = "1";
        public ConfigurationElement ModDrawReverse { get; set; } = "1";

        public ConfigurationElement ModUse2x { get; set; } = "0";

        public ConfigurationElement ModSkinPath { get; set; } = "";

        public ConfigurationElement ModIsHorizon { get; set; } = "1";

        public ConfigurationElement EnableScaleClipOutputImageFile { get; set; } = "1";

        public ConfigurationElement EnableListenOutputImageFile { get; set; } = "1";
        
        public ConfigurationElement PPShowJsonConfigFilePath { set; get; } = @"..\PPShowConfig.json";
        public ConfigurationElement PPShowAllowDumpInfo { get; set; } = "0";
        
        /// <summary>
        /// 当前谱面背景文件保存路径
        /// </summary>
        public ConfigurationElement OutputBackgroundImageFilePath { get; set; } = @"..\output_result.png";

        #endregion Options

        public event Action OnSettingChanged;

        private UsingSource source = UsingSource.None;

        private PluginConfigurationManager manager;
        
        private RestfulAPIGetterReceiver receiver;

        private string OsuSyncPath;

        #region DDPR_field

        private string current_bg_file_path;

        #endregion

        public ModsPictureGenerator mods_pic_output;

        public BeatmapInfomationGeneratorPlugin PPShowPluginInstance { get; private set; }

        public OsuLiveStatusPanelPlugin() : base("OsuLiveStatusPanelPlugin", "MikiraSora & KedamavOvO >///<")
        {
            I18n.Instance.ApplyLanguage(new Languages());
        }

        public override void OnEnable()
        {
            base.EventBus.BindEvent<PluginEvents.LoadCompleteEvent>(OsuLiveStatusPanelPlugin_onLoadComplete);
            base.EventBus.BindEvent<PluginEvents.InitCommandEvent>(OsuLiveStatusPanelPlugin_onInitCommand);

            manager = new PluginConfigurationManager(this);
            manager.AddItem(this);

            Sync.Tools.IO.CurrentIO.WriteColor(this.Name + " by " + this.Author, System.ConsoleColor.DarkCyan);
        }

        private void OsuLiveStatusPanelPlugin_onInitCommand(PluginEvents.InitCommandEvent @event)
        {
            @event.Commands.Dispatch.bind("olsp", (args) =>
            {
                if (args.Count()==0)
                {
                    Help();
                    return true;
                }

                switch (args[0])
                {
                    case "help":
                        Help();
                        break;
                    case "get":
                        //IO.CurrentIO.WriteColor($"{args[1]}\t=\t{GetData(args[1])}", ConsoleColor.Cyan);
                        break;
                    case "restart":
                        ReInitizePlugin();
                        break;
                    case "status":
                        Status();
                        break;
                    default:
                        break;
                }

                return true;
            }, COMMAND_DESC);
        }

        private void OsuLiveStatusPanelPlugin_onLoadComplete(PluginEvents.LoadCompleteEvent evt)
        {
            SyncHost host = evt.Host;

            SetupPlugin(host);
            
            RegisterProcess(new ProcessEvent.BeatmapPictureOutputProcessRevevices(OutputBackgroundImageFilePath, EnableListenOutputImageFile, EnableScaleClipOutputImageFile, int.Parse(Width), int.Parse(Height)));
            
            
            if (EnableOutputModPicture=="1")
                RegisterProcess(new ProcessEvent.ModsPictureOutputProcessReveicer(OutputModImageFilePath, ModSkinPath, int.Parse(ModUnitPixel), int.Parse(ModUnitOffset), ModIsHorizon == "1", ModUse2x == "1", ModSortReverse == "1", ModDrawReverse == "1"));
            
            receiver = new RestfulAPIGetterReceiver();
            
            RegisterProcess(receiver);
        }

        #region Commands

        public void ReInitizePlugin()
        {
            TermPlugin();

            SetupPlugin(getHoster());

            IO.CurrentIO.WriteColor(REINIT_SUCCESS, ConsoleColor.Green);
        }

        public void Help()
        {
            IO.CurrentIO.WriteColor(COMMAND_HELP, ConsoleColor.Yellow);
        }

        public void Status()
        {
            IO.CurrentIO.WriteColor(string.Format(CONNAND_STATUS, source.ToString(),PPShowJsonConfigFilePath), ConsoleColor.Green);
        }

        #endregion
        
        #region ProcessEvent

        public void RegisterProcess(ProcessRecevierBase process)
        {
            process._dispatcher = EventBus;
            process.OnEventRegister(EventBus);
        }

        public void RaiseProcessEvent<T>(T sender) where T: ProcessEventBase
        {
            //IO.CurrentIO.WriteColor($"[Process]{sender.ToString()}",ConsoleColor.Cyan);
            EventBus.RaiseEvent<T>(sender);
        }

        #endregion

        private void SetupPlugin(SyncHost host)
        {
            OsuSyncPath = Directory.GetParent(Environment.CurrentDirectory).FullName + @"\";

            //init PPShow
            PPShowPluginInstance = new BeatmapInfomationGeneratorPlugin(this,PPShowJsonConfigFilePath);

            source = UsingSource.None;

            try
            {
                if (((string)AllowUsedNowPlaying).Trim() == "1")
                {
                    TryRegisterSourceFromNowPlaying(host);
                }
                else if (((string)AllowUsedOsuRTDataProvider).Trim() == "1")
                {
                    TryRegisterSourceFromOsuRTDataProvider(host);
                }
            }
            catch (Exception e)
            {
                IO.CurrentIO.WriteColor($"[OsuLiveStatusPanelPlugin]{LOAD_PLUGIN_DEPENDENCY_FAILED}:{e.Message}", ConsoleColor.Red);
                source = UsingSource.None;
            }

            if (source == UsingSource.None)
            {
                IO.CurrentIO.WriteColor($"[OsuLiveStatusPanelPlugin]{INIT_PLUGIN_FAILED_CAUSE_NO_DEPENDENCY}", ConsoleColor.Red);
            }
            else
            {
                IO.CurrentIO.WriteColor($"[OsuLiveStatusPanelPlugin]{INIT_SUCCESS}", ConsoleColor.Green);
            }

            CleanOsuStatus();
        }

        private void TermPlugin()
        {
            //source clean itself
            SourceWrapper.Detach();
        }

        public void TryRegisterSourceFromOsuRTDataProvider(SyncHost host)
        {
            foreach (var plugin in host.EnumPluings())
            {
                if (plugin.Name == "OsuRTDataProvider")
                {
                    IO.CurrentIO.WriteColor($"[OsuLiveStatusPanelPlugin]{OSURTDP_FOUND}", ConsoleColor.Green);
                    OsuRTDataProvider.OsuRTDataProviderPlugin reader = plugin as OsuRTDataProvider.OsuRTDataProviderPlugin;

                    SourceWrapper = new OsuRTDataProviderWrapper(reader, this);

                    if (SourceWrapper.Attach())
                    {
                        source = UsingSource.OsuRTDataProvider;
                    }

                    return;
                }
            }

            IO.CurrentIO.WriteColor($"[OsuLiveStatusPanelPlugin]{OSURTDP_NOTFOUND}", ConsoleColor.Red);

            source = UsingSource.None;
        }

        public void TryRegisterSourceFromNowPlaying(SyncHost host)
        {
            foreach (var plugin in host.EnumPluings())
            {
                if (plugin.Name == "Now Playing")
                {
                    IO.CurrentIO.WriteColor($"[OsuLiveStatusPanelPlugin]{NOWPLAYING_FOUND}.", ConsoleColor.Green);
                    NowPlaying.NowPlaying np = plugin as NowPlaying.NowPlaying;

                    SourceWrapper = new NowPlayingWrapper(np, this);

                    if (SourceWrapper.Attach())
                    {
                        source = UsingSource.NowPlaying;
                    }

                    return;
                }
            }

            IO.CurrentIO.WriteColor($"[OsuLiveStatusPanelPlugin]{NOWPLAYING_NOTFOUND}", ConsoleColor.Red);

            source = UsingSource.None;
        }

        #region Kernal

        private void CleanOsuStatus()
        {
            IO.CurrentIO.WriteColor($"[OsuLiveStatusPanelPlugin]{CLEAN_STATUS}", ConsoleColor.Green);

            OutputInfomationClean();
        }

        private void OutputInfomationClean()
        {
            this.RaiseProcessEvent(new ClearProcessEvent());
        }
        
        #region tool func
        
        #region DDPR

        static readonly string[] ppshow_provideable_data_array = new[] { "ar", "cs", "od", "hp", "pp", "beatmap_setid", "version", "title_avaliable", "artist_avaliable", "beatmap_setlink", "beatmap_link", "beatmap_id", "min_bpm", "max_bpm", "speed_stars", "aim_stars", "stars", "mods", "title", "creator", "max_combo", "artist", "circles", "spinners" };

        private Dictionary<string, Func<OsuLiveStatusPanelPlugin, string>> DataGetterMap = new Dictionary<string, Func<OsuLiveStatusPanelPlugin, string>>()
        {
            {"olsp_bg_path",o=>o.current_bg_file_path},
            {"olsp_status",o=>o.SourceWrapper?.CurrentOutputType.ToString()},
            {"olsp_bg_save_path",o=>o.OutputBackgroundImageFilePath },
            {"olsp_mod_save_path",o=>o.OutputModImageFilePath },
            {"olsp_ppshow_config_path",o=>o.PPShowJsonConfigFilePath },
            {"olsp_source",o=>o.source.ToString() }
        };

        private bool GetCurrentPluginData(string name, out string value)
        {
            value = null;

            if (DataGetterMap.TryGetValue(name, out var picker))
            {
                value = picker(this);
                return true;
            }

            return false;
        }

        public object GetData(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            if (GetCurrentPluginData(name,out string result))
                return result;
            
            return receiver.GetData(name);
        }

        public IEnumerable<string> EnumProvidableDataName()
        {
            foreach (var name in DataGetterMap.Keys)
                yield return name;

            foreach (var name in ppshow_provideable_data_array)
                yield return name;
        }

        #endregion

        public void onConfigurationLoad()
        {

        }

        public void onConfigurationSave()
        {

        }

        public void onConfigurationReload()
        {
            mods_pic_output = null;
            OnSettingChanged?.Invoke();
        }

        public override void OnExit()
        {
            base.OnExit();
            OutputInfomationClean();
        }

        #endregion tool func

        #endregion Kernal
    }
}