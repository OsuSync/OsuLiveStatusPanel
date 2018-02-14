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
                        IO.CurrentIO.WriteColor($"{args[1]}\t=\t{GetData(args[1])}", ConsoleColor.Cyan);
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

            if (EnableOutputModPicture=="1"&&mods_pic_output==null)
            {
                //init mods_pic_output
                TryCreateModsPictureGenerator(out mods_pic_output);
            }

            OutputBeatmapInfomation(current_beatmap, mod);

            if (current_beatmap.OutputType==OutputType.Play)
            {
                if (mods_pic_output != null)
                {
                    var mod_list = OsuRTDataProviderWrapperInstance.current_mod.Name.Split(',');

                    using (Bitmap result = mods_pic_output.GenerateModsPicture(mod_list))
                    {
                        result.Save(OutputModImageFilePath, ImageFormat.Png);
                    }
                }
            }
            else
            {
                //clean
                if (File.Exists(OutputModImageFilePath))
                {
                    File.Delete(OutputModImageFilePath);
                }
            }

            return true;
        }

        private void OutputInfomationClean()
        {
            PPShowPluginInstance?.Output(OutputType.Listen,string.Empty, string.Empty);

            current_bg_file_path = string.Empty;

            if (File.Exists(OutputModImageFilePath))
            {
                File.Delete(OutputModImageFilePath);
            }

            if (File.Exists(OutputBackgroundImageFilePath))
            {
                File.Delete(OutputBackgroundImageFilePath);
            }

            EventBus.RaiseEvent(new OutputInfomationEvent(OutputType.Listen));
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

            if(!OutputInfomation(current_beatmap.OutputType, beatmap_osu_file, mod))
            {
                IO.CurrentIO.WriteColor($"[OsuLiveStatusPanelPlugin]Cant output info {current_beatmap.BeatmapSetId}.", ConsoleColor.Yellow);
                return;
            }

            #region OutputBackgroundImage

            var match = Regex.Match(osuFileContent, @"\""((.+?)\.((jpg)|(png)|(jpeg)))\""", RegexOptions.IgnoreCase);
            string bgPath = current_bg_file_path = beatmap_folder + @"\" + match.Groups[1].Value;

            if (!File.Exists(bgPath))
            {
                IO.CurrentIO.WriteColor($"[OsuLiveStatusPanelPlugin::OutputImage]{IMAGE_NOT_FOUND}{bgPath}", ConsoleColor.Yellow);
            }

            if (EnableListenOutputImageFile == "1" || current_beatmap.OutputType == OutputType.Play)
            {
                try
                {
                    if (EnableScaleClipOutputImageFile == "1")
                    {

                        using (Bitmap bitmap = GetFixedResolutionBitmap(bgPath, int.Parse(Width), int.Parse(Height)))
                        using (var fp = File.Open(OutputBackgroundImageFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                            bitmap.Save(fp, ImageFormat.Png);
                    }
                    else
                    {
                        //Copy image file.
                        using (var dst = File.Open(OutputBackgroundImageFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                        using (var src = File.Open(bgPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                            src.CopyTo(dst);
                    }
                }
                catch (Exception e)
                {
                    IO.CurrentIO.WriteColor($"[OsuLiveStatusPanelPlugin]{CANT_PROCESS_IMAGE}:{e.Message}", ConsoleColor.Red);
                }
            }


            #endregion

            #endregion GetInfo

            EventBus.RaiseEvent(new OutputInfomationEvent(current_beatmap.OutputType));

            IO.CurrentIO.WriteColor($"[OsuLiveStatusPanelPlugin]Done!output_type:{current_beatmap.OutputType} setid:{current_beatmap.BeatmapSetId} mod:{mod}", ConsoleColor.Green);
        }

        #region tool func

        private void TryCreateModsPictureGenerator(out ModsPictureGenerator modsPictureGenerator)
        {
            Process process = Process.GetProcessesByName("osu!")?.First();
            if (process==null)
            {
                modsPictureGenerator = null;
                return;
            }

            string osu_path = Path.GetDirectoryName(process.MainModule.FileName);
            string osu_config_file = Path.Combine(osu_path, $"osu!.{Environment.UserName}.cfg");
            string using_skin_name=string.Empty;

            var lines = File.ReadLines(osu_config_file);
            foreach (var line in lines)
            {
                if (line.Trim().StartsWith("Skin =")|| line.Trim().StartsWith("Skin="))
                {
                    using_skin_name = line.Split('=')[1].Trim();
                }
            }

            if (string.IsNullOrWhiteSpace(using_skin_name))
            {
                modsPictureGenerator = null;
                return;
            }

            string using_skin_path = Path.Combine(osu_path,"Skins", using_skin_name);

            IO.CurrentIO.WriteColor($"[MPG]using_skin_path={using_skin_path}",ConsoleColor.Cyan);

            modsPictureGenerator = new ModsPictureGenerator(using_skin_path, ModSkinPath, int.Parse(ModUnitPixel), int.Parse(ModUnitOffset), ModIsHorizon == "1",ModUse2x=="1",ModSortReverse=="1",ModDrawReverse=="1");
        }

        private bool OutputInfomation(OutputType output_type, string osu_file_path,string mod_list)
        {
            return PPShowPluginInstance.Output(output_type,osu_file_path, mod_list);
        }

        private Bitmap GetFixedResolutionBitmap(string file,int dstw,int dsth)
        {
            float r = dstw / (float)dsth;
            var dbitmap = new Bitmap(dstw, dsth);

            using (var sbitmap = new Bitmap(file))
            {
                float w = 0, h = 0;
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

                float scalex = sdata.Width / (float)ddata.Width;
                float scaley = sdata.Height / (float)ddata.Height;

                unsafe
                {
                    byte* sptr = (byte*)(sdata.Scan0);
                    byte* dptr = (byte*)(ddata.Scan0);
                    byte* sp_up,sp_down, sp_left,sp_right;
                    int si = 0, sj = 0;

                    float t;
                    float u, v, omu,omv;
                    Vector4 abcd;
                    Vector4 g1, g2, g3;

                    for (int i = 0; i < ddata.Height; i++, dptr += ddata.Stride - ddata.Width * 3)
                    {
                        t = i * scaley;
                        si = (int)(t);
                        v = t-si;

                        if ((si + 1) == sdata.Height) continue;

                        for (int j = 0; j < ddata.Width; j++,dptr += 3)
                        {
                            t = j * scalex;
                            sj = (int)(t);
                            u = t - sj;
                            if ((sj + 1) == sdata.Width) continue;

                            omu = 1 - u;
                            omv = 1 - v;

                            abcd.X = omu * omv;abcd.Y = u * v;abcd.Z = omu * v;abcd.W = omv * u;

                            sp_up    = sptr + ((si - 0) * sdata.Stride + (sj - 0) * 3);//left up 0,0
                            sp_down  = sptr + ((si + 1) * sdata.Stride + (sj + 1) * 3);//right down 1,1
                            sp_left  = sptr + ((si + 1) * sdata.Stride + (sj - 0) * 3);//left down 0,1
                            sp_right = sptr + ((si - 0) * sdata.Stride + (sj + 1) * 3);//rigth up 1,0

                            g1.X = sp_up[0]; g1.Y = sp_down[0]; g1.Z = sp_left[0]; g1.W = sp_right[0];
                            g2.X = sp_up[1]; g2.Y = sp_down[1]; g2.Z = sp_left[1]; g2.W = sp_right[1];
                            g3.X = sp_up[2]; g3.Y = sp_down[2]; g3.Z = sp_left[2]; g3.W = sp_right[2];

                            dptr[0] = (byte)(Vector4.Dot(g1, abcd));
                            dptr[1] = (byte)(Vector4.Dot(g2, abcd));
                            dptr[2] = (byte)(Vector4.Dot(g3, abcd));
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
            {
                return null;
            }

            if (GetCurrentPluginData(name,out string result))
            {
                return result;
            }

            //try get from ppshow
            return PPShowPluginInstance?.GetData(name);
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
            if (source==UsingSource.OsuRTDataProvider)
            {
                TryCreateModsPictureGenerator(out mods_pic_output);
            }
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