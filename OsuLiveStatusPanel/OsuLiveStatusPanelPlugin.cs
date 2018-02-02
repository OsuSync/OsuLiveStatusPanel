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

namespace OsuLiveStatusPanel
{
    [SyncPluginID("dcca15cb-8b8c-4375-934c-2c2b34862e33","1.0.6")]
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

        public ConfigurationElement EnableGenerateNormalImageFile { get; set; } = "1";

        public ConfigurationElement EnableListenOutputImageFile { get; set; } = "1";
        
        public ConfigurationElement PPShowJsonConfigFilePath { set; get; } = @"..\PPShowConfig.json";
        public ConfigurationElement PPShowAllowDumpInfo { get; set; } = "0"; 
         
        /// <summary>
        /// 当前谱面背景文件保存路径
        /// </summary>
        public ConfigurationElement OutputBackgroundImageFilePath { get; set; } = @"..\output_result.png"; 
        
        #endregion Options

        private UsingSource source = UsingSource.None;

        private PluginConfigurationManager manager;

        private string OsuSyncPath;

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
            @event.Commands.Dispatch.bind("livestatuspanel", (args) =>
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

        private void SetupPlugin(SyncHost host)
        {
            OsuSyncPath = Directory.GetParent(Environment.CurrentDirectory).FullName + @"\";

            //init PPShow
            PPShowPluginInstance = new BeatmapInfomationGeneratorPlugin(PPShowJsonConfigFilePath);

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

        public void OnBeatmapChanged(SourceWrapperBase source,BeatmapChangedParameter evt)
        {
            if (source!=SourceWrapper)
            {
                return;
            }

            BeatmapEntry new_beatmap = evt?.beatmap;

            var osu_process = Process.GetProcessesByName("osu!")?.First();

            if (new_beatmap == null || osu_process == null)
            {
                if (osu_process == null)
                    IO.CurrentIO.WriteColor($"[OsuLiveStatusPanelPlugin]{OSU_PROCESS_NOTFOUND}!", ConsoleColor.Red);
                CleanOsuStatus();
                return;
            }

            TryApplyBeatmapInfomation(new_beatmap);
        }

        private void CleanOsuStatus()
        {
            IO.CurrentIO.WriteColor($"[OsuLiveStatusPanelPlugin]{CLEAN_STATUS}", ConsoleColor.Green);

            OutputInfomationClean();

            using (var fp = File.Open(OutputBackgroundImageFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
            }
        }

        private void TryApplyBeatmapInfomation(object obj)
        {
            BeatmapEntry beatmap = obj as BeatmapEntry;
            if (!(source == UsingSource.OsuRTDataProvider ? ApplyBeatmapInfomationforOsuRTDataProvider(beatmap) : ApplyBeatmapInfomationforNowPlaying(beatmap)))
            {
                CleanOsuStatus();
            }
        }

        private bool ApplyBeatmapInfomationforOsuRTDataProvider(BeatmapEntry current_beatmap)
        {
            OsuRTDataProviderWrapper OsuRTDataProviderWrapperInstance = SourceWrapper as OsuRTDataProviderWrapper;

            string mod = string.Empty;
            //添加Mods
            if (OsuRTDataProviderWrapperInstance.current_mod.Mod != OsuRTDataProvider.Mods.ModsInfo.Mods.Unknown)
            {
                //处理不能用的PP
                mod = $"{OsuRTDataProviderWrapperInstance.current_mod.ShortName}";
            }

            OutputBeatmapInfomation(current_beatmap, mod);

            return true;
        }

        private void OutputInfomationClean()
        {
            PPShowPluginInstance?.Output(OutputType.Listen,string.Empty, string.Empty);
        }

        private bool ApplyBeatmapInfomationforNowPlaying(BeatmapEntry current_beatmap)
        {
            #region GetInfo

            string beatmap_osu_file = string.Empty;

            beatmap_osu_file = current_beatmap.OsuFilePath;

            if (string.IsNullOrWhiteSpace(beatmap_osu_file))
            {
                IO.CurrentIO.WriteColor($"[OsuLiveStatusPanelPlugin]{NO_BEATMAP_PATH}", ConsoleColor.Red);
                return false;
            }

            OutputBeatmapInfomation(current_beatmap);

            return true;
        }

        public void OutputBeatmapInfomation(BeatmapEntry current_beatmap, string mod = "")
        {
            string beatmap_osu_file = current_beatmap.OsuFilePath;
            string osuFileContent = File.ReadAllText(beatmap_osu_file);
            string beatmap_folder = Directory.GetParent(beatmap_osu_file).FullName;
            
            OutputInfomation(current_beatmap.OutputType, beatmap_osu_file, mod);

            #region OutputBackgroundImage

            var match = Regex.Match(osuFileContent, @"\""((.+?)\.((jpg)|(png)|(jpeg)))\""",RegexOptions.IgnoreCase);
            string bgPath = beatmap_folder + @"\" + match.Groups[1].Value;

            if (!File.Exists(bgPath))
            {
                IO.CurrentIO.WriteColor($"[OsuLiveStatusPanelPlugin::OutputImage]{IMAGE_NOT_FOUND}{bgPath}", ConsoleColor.Yellow);
            }

            if (EnableListenOutputImageFile=="1"||current_beatmap.OutputType==OutputType.Play)
            {
                if (EnableGenerateNormalImageFile == "1")
                {
                    try
                    {

                        using (Bitmap bitmap = GetFixedResolutionBitmap(bgPath, int.Parse(Width), int.Parse(Height)))
                        using (var fp = File.Open(OutputBackgroundImageFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                            bitmap.Save(fp, ImageFormat.Png);
 
                        //using (var dst = File.Open(OutputBackgroundImageFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                        //using (var src = File.Open(bgPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        //    src.CopyTo(dst);
                    }
                    catch (Exception e)
                    {
                        IO.CurrentIO.WriteColor($"[OsuLiveStatusPanelPlugin]{CANT_PROCESS_IMAGE}:{e.Message}", ConsoleColor.Red);
                    }
                }
            }

            #endregion

            #endregion GetInfo

            IO.CurrentIO.WriteColor($"[OsuLiveStatusPanelPlugin]Done!output_type:{current_beatmap.OutputType} setid:{current_beatmap.BeatmapSetId} mod:{mod}", ConsoleColor.Green);
        }

        #region tool func

        private void OutputInfomation(OutputType output_type, string osu_file_path,string mod_list)
        {
            PPShowPluginInstance.Output(output_type,osu_file_path, mod_list);
        }

        private Bitmap GetFixedResolutionBitmap(string file,int dstw,int dsth)
        {
            double r = dstw / (double)dsth;
            var dbitmap = new Bitmap(dstw, dsth);

            using (var sbitmap = new Bitmap(file))
            {
                double w = 0, h = 0;
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                w = sbitmap.Width * r;
                if (w > sbitmap.Width)
                {
                    w = sbitmap.Width;
                    h = sbitmap.Width / r;
                }
                if(h > sbitmap.Height)
                {
                    w = sbitmap.Height * r;
                    h = sbitmap.Height;
                }

                Rectangle rectangle = new Rectangle();
                rectangle.Width = (int)w;
                rectangle.Height = (int)h;
                rectangle.X = (sbitmap.Width - rectangle.Width) / 2;
                rectangle.Y = (sbitmap.Height - rectangle.Height) / 2;

                var sdata = sbitmap.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                var ddata = dbitmap.LockBits(new Rectangle(0, 0, dstw, dsth), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

                double scalex = sdata.Width / (double)ddata.Width;
                double scaley = sdata.Height / (double)ddata.Height;

                unsafe
                {
                    byte* sptr = (byte*)(sdata.Scan0);
                    byte* dptr = (byte*)(ddata.Scan0);
                    byte* sp_up,sp_down, sp_left,sp_right;
                    int si = 0, sj = 0;

                    for (int i = 0; i < ddata.Height; i++, dptr += ddata.Stride - ddata.Width * 3)
                    {
                        si = (int)(i * scaley);
                        if (si == 0 || (si + 1) == sdata.Height) continue;

                        for (int j = 0; j < ddata.Width; j++,dptr += 3)
                        {
                            sj = (int)(j * scalex);
                            if (sj == 0 || (sj + 1) == sdata.Width) continue;

                            int a = 0, b = 0, c = 0;

                            sp_up = sptr + ((si - 1) * sdata.Stride + sj * 3);
                            sp_down  = sptr + ((si + 1) * sdata.Stride + sj * 3);
                            sp_left  = sptr + (si * sdata.Stride + (sj - 1) * 3);
                            sp_right = sptr + (si * sdata.Stride + (sj + 1) * 3);

                            a = sp_up[0] + sp_down[0] + sp_left[0] + sp_right[0];
                            b = sp_up[1] + sp_down[1] + sp_left[1] + sp_right[1];
                            c = sp_up[2] + sp_down[2] + sp_left[2] + sp_right[2];

                            dptr[0] = (byte)(a >>2);
                            dptr[1] = (byte)(b >>2);
                            dptr[2] = (byte)(c >>2);
                        }
                    }
                }

                sbitmap.UnlockBits(sdata);
                dbitmap.UnlockBits(ddata);

                stopwatch.Stop();
                IO.CurrentIO.Write($"[OLSP]线性插值:{stopwatch.ElapsedMilliseconds}ms");
            }
            return dbitmap;
        }

        public void onConfigurationLoad()
        {

        }

        public void onConfigurationSave()
        {

        }

        public void onConfigurationReload()
        {
            ReInitizePlugin();
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