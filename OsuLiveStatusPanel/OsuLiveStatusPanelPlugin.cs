using OsuLiveStatusPanel.Gui;
using OsuLiveStatusPanel.Mods;
using OsuLiveStatusPanel.PPShow;
using OsuLiveStatusPanel.SourcesWrapper;
using OsuLiveStatusPanel.SourcesWrapper.DPMP;
using OsuLiveStatusPanel.SourcesWrapper.NP;
using OsuLiveStatusPanel.SourcesWrapper.ORTDP;
using Sync;
using Sync.Plugins;
using Sync.Tools;
using Sync.Tools.ConfigurationAttribute;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using static OsuLiveStatusPanel.Languages;

namespace OsuLiveStatusPanel
{
    [SyncPluginID("dcca15cb-8b8c-4375-934c-2c2b34862e33", "2.0.0")]
    public class OsuLiveStatusPanelPlugin : Plugin, IConfigurable
    {
        private enum UsingSource
        {
            OsuRTDataProvider,
            NowPlaying,
            DifficultParamModifyPlugin,
            None
        }

        private SourceWrapperBase SourceWrapper;

        #region Options

        [List(AllowMultiSelect = false, IgnoreCase = true, ValueList = new[] { "ortdp", "np" ,"dpmp"})]
        public ConfigurationElement BeatmapSourcePlugin { get; set; } = "ortdp";

        [Integer]
        public ConfigurationElement Width { get; set; } = "1920";

        [Integer]
        public ConfigurationElement Height { get; set; } = "1080";

        [Bool]
        public ConfigurationElement EnableOutputModPicture { get; set; } = "False";

        [Path(IsDirectory = false)]
        public ConfigurationElement OutputModImageFilePath { get; set; } = @"..\output_mod.png";

        [Integer]
        public ConfigurationElement ModUnitPixel { get; set; } = "90";

        [Integer]
        public ConfigurationElement ModUnitOffset { get; set; } = "10";

        [Bool]
        public ConfigurationElement ModSortReverse { get; set; } = "True";

        [Bool]
        public ConfigurationElement ModDrawReverse { get; set; } = "True";

        [Bool]
        public ConfigurationElement ModUse2x { get; set; } = "False";

        [Path(IsDirectory = true)]
        public ConfigurationElement ModSkinPath { get; set; } = "";

        [Bool]
        public ConfigurationElement ModIsHorizon { get; set; } = "True";

        [Bool]
        public ConfigurationElement EnableScaleClipOutputImageFile { get; set; } = "True";

        [Bool]
        public ConfigurationElement EnableListenOutputImageFile { get; set; } = "True";

        [ConfigEditor(IsDirectory = false)]
        public ConfigurationElement PPShowJsonConfigFilePath { set; get; } = @"..\PPShowConfig.json";

        [Bool]
        public ConfigurationElement EnableOutputBackgroundImage { get; set; } = "True";

        /// <summary>
        /// 当前谱面背景文件保存路径
        /// </summary>
        [Path(IsDirectory = false)]
        public ConfigurationElement OutputBackgroundImageFilePath { get; set; } = @"..\output_result.png";

        #endregion Options

        public event Action OnSettingChanged;

        private UsingSource source = UsingSource.None;

        private PluginConfigurationManager manager;

        private Logger logger = new Logger("OsuLiveStatusPanel");

        #region DDPR_field

        private string current_bg_file_path;

        #endregion DDPR_field

        public ModsPictureGenerator mods_pic_output;

        public InfoOutputterWrapper PPShowPluginInstance { get; private set; }

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
                if (args.Count() == 0)
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
                        Log.Output($"{args[1]}\t=\t{GetData(args[1])}");
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

            Log.Output(REINIT_SUCCESS);
        }

        public void Help()
        {
            Log.Output(COMMAND_HELP);
        }

        public void Status()
        {
            Log.Output(string.Format(CONNAND_STATUS, source.ToString(), PPShowJsonConfigFilePath));
        }

        #endregion Commands

        private void SetupPlugin(SyncHost host)
        {
            //init PPShow
            PPShowPluginInstance = new InfoOutputterWrapper(PPShowJsonConfigFilePath);

            source = UsingSource.None;

            try
            {
                switch (BeatmapSourcePlugin.ToString().ToLower().Trim())
                {
                    case "np":
                        TryRegisterSourceFromNowPlaying(host);
                        break;

                    case "ortdp":
                        TryRegisterSourceFromOsuRTDataProvider(host);
                        break;

                    case "dpmp":
                        TryRegisterSourceFromDifficultParamModifyPlugin(host);
                        break;

                    default:
                        break;
                }

                logger.LogInfomation($"Source:{BeatmapSourcePlugin} Loaded:{SourceWrapper.ToString()}");
            }
            catch (Exception e)
            {
                Log.Error($"[OsuLiveStatusPanelPlugin]{LOAD_PLUGIN_DEPENDENCY_FAILED}:{e.Message}");
                source = UsingSource.None;
            }

            if (source == UsingSource.None)
            {
                Log.Error(INIT_PLUGIN_FAILED_CAUSE_NO_DEPENDENCY);
            }
            else
            {
                Log.Output(INIT_SUCCESS);
            }

            Plugin config_gui = getHoster().EnumPluings().FirstOrDefault(p => p.Name == "ConfigGUI");
            if(config_gui!=null)
                GuiRegisterHelper.RegisterConfigGui(config_gui,PPShowPluginInstance);

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
                    Log.Output(OSURTDP_FOUND);
                    OsuRTDataProvider.OsuRTDataProviderPlugin reader = plugin as OsuRTDataProvider.OsuRTDataProviderPlugin;

                    if (reader.ModsChangedAtListening)
                    {
                        SourceWrapper = new RealtimeDataProviderModsWrapper(reader, this);
                    }
                    else
                    {
                        SourceWrapper = new OsuRTDataProviderWrapper(reader, this);
                    }

                    if (SourceWrapper.Attach())
                    {
                        source = UsingSource.OsuRTDataProvider;
                    }

                    return;
                }
            }

            Log.Error(OSURTDP_NOTFOUND);

            source = UsingSource.None;
        }

        public void TryRegisterSourceFromDifficultParamModifyPlugin(SyncHost host)
        {
            foreach (var plugin in host.EnumPluings())
            {
                if (plugin.Name == "DifficultParamModifyPlugin")
                {
                    Log.Output($"发现dpmp插件");
                    DifficultParamModifyPlugin.DifficultParamModifyPlugin reader = plugin as DifficultParamModifyPlugin.DifficultParamModifyPlugin;

                    SourceWrapper = new DifficultParamModifyPluginSourceWrapper(reader, this);

                    if (SourceWrapper.Attach())
                    {
                        source = UsingSource.DifficultParamModifyPlugin;
                    }

                    return;
                }
            }

            Log.Error($"没发现dpmp插件");

            source = UsingSource.None;
        }

        public void TryRegisterSourceFromNowPlaying(SyncHost host)
        {
            foreach (var plugin in host.EnumPluings())
            {
                if (plugin.Name == "Now Playing")
                {
                    Log.Output(NOWPLAYING_FOUND);
                    NowPlaying.NowPlaying np = plugin as NowPlaying.NowPlaying;

                    SourceWrapper = new NowPlayingWrapper(np, this);

                    if (SourceWrapper.Attach())
                    {
                        source = UsingSource.NowPlaying;
                    }

                    return;
                }
            }

            Log.Error(NOWPLAYING_NOTFOUND);

            source = UsingSource.None;
        }

        #region Kernal

        public void OnBeatmapChanged(SourceWrapperBase source, BeatmapChangedParameter evt)
        {
            if (source != SourceWrapper)
            {
                return;
            }

            BeatmapEntry new_beatmap = evt?.beatmap;

            var processes = Process.GetProcessesByName("osu!");
            if (processes.Length == 0) return;

            var osu_process = processes?.First();

            if (new_beatmap == null || osu_process == null)
            {
                if (osu_process == null)
                    Log.Error(OSU_PROCESS_NOTFOUND);
                CleanOsuStatus();
                return;
            }

            TryApplyBeatmapInfomation(new_beatmap);
        }

        private void CleanOsuStatus()
        {
            Log.Output(CLEAN_STATUS);

            if (File.Exists(OutputModImageFilePath))
            {
                File.Delete(OutputModImageFilePath);
            }

            OutputInfomationClean();
        }

        private void TryApplyBeatmapInfomation(object obj)
        {
            BeatmapEntry beatmap = obj as BeatmapEntry;
            bool apply_result=false;

            switch (source)
            {
                case UsingSource.OsuRTDataProvider:
                    apply_result = ApplyBeatmapInfomationforOsuRTDataProvider(beatmap);
                    break;
                case UsingSource.NowPlaying:
                    apply_result = ApplyBeatmapInfomationforNowPlaying(beatmap);
                    break;
                case UsingSource.DifficultParamModifyPlugin:
                    apply_result=ApplyBeatmapInfomationforDifficultParamModifyPlugin(beatmap);
                    break;
                case UsingSource.None:
                    break;
                default:
                    break;
            }

            if (!apply_result)
                CleanOsuStatus();
        }

        private bool ApplyBeatmapInfomationforDifficultParamModifyPlugin(BeatmapEntry current_beatmap)
        {
            var wrapper = SourceWrapper as DifficultParamModifyPluginSourceWrapper;

            ModsInfo mod = default(ModsInfo);

            //添加Mods
            if (wrapper.CurrentMod.Mod != Mods.ModsInfo.Mods.Unknown)
            {
                //处理不能用的PP
                mod.Mod = wrapper.CurrentMod.Mod;
            }

            OutputBeatmapInfomation(current_beatmap, mod);

            return true;
        }

        private bool ApplyBeatmapInfomationforOsuRTDataProvider(BeatmapEntry current_beatmap)
        {
            RealtimeDataProvideWrapperBase OsuRTDataProviderWrapperInstance = SourceWrapper as RealtimeDataProvideWrapperBase;

            ModsInfo mod = default(ModsInfo);

            //添加Mods
            if (OsuRTDataProviderWrapperInstance.current_mod.Mod != OsuRTDataProvider.Mods.ModsInfo.Mods.Unknown)
            {
                //处理不能用的PP
                mod.Mod = (ModsInfo.Mods)((uint)OsuRTDataProviderWrapperInstance.current_mod.Mod);
            }

            OutputBeatmapInfomation(current_beatmap, mod);

            return true;
        }

        private void OutputInfomationClean()
        {
            PPShowPluginInstance?.Output(OutputType.Listen, string.Empty, ModsInfo.Empty);

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
                Log.Error(NO_BEATMAP_PATH);
                return false;
            }

            OutputBeatmapInfomation(current_beatmap, ModsInfo.Empty);

            return true;
        }

        private Stopwatch sw = new Stopwatch();

        public void OutputBeatmapInfomation(BeatmapEntry current_beatmap, ModsInfo mod)
        {
            sw.Restart();

            string beatmap_osu_file = current_beatmap.OsuFilePath;
            if (string.IsNullOrEmpty(beatmap_osu_file)) return;
            string osuFileContent = File.ReadAllText(beatmap_osu_file);
            string beatmap_folder = Directory.GetParent(beatmap_osu_file).FullName;

            if (!OutputInfomation(current_beatmap.OutputType, current_beatmap, mod))
            {
                Log.Warn($"Cant output info {current_beatmap.BeatmapSetId}.");
                return;
            }

            #region OutputBackgroundImage

            var match = Regex.Match(osuFileContent, @"\""((.+?)\.((jpg)|(png)|(jpeg)))\""", RegexOptions.IgnoreCase);
            string bgPath = beatmap_folder + @"\" + match.Groups[1].Value;

            if (bgPath != current_bg_file_path)
                OutputBackgroundImage(bgPath, current_beatmap.OutputType);

            current_bg_file_path = bgPath;

            #endregion OutputBackgroundImage

            #endregion GetInfo

            #region Mods Ouptut

            if (EnableOutputModPicture.ToBool() && mods_pic_output == null)
            {
                //init mods_pic_output
                TryCreateModsPictureGenerator(out mods_pic_output);
            }

            if (mods_pic_output != null)
            {
                var mod_list = mod.Name.Split(',');

                using (Bitmap result = mods_pic_output.GenerateModsPicture(mod_list))
                {
                    result.Save(OutputModImageFilePath, ImageFormat.Png);
                }
            }

            #endregion Mods Ouptut

            EventBus.RaiseEvent(new OutputInfomationEvent(current_beatmap.OutputType));

            Log.Output($"Done!time:{sw.ElapsedMilliseconds}ms output_type:{current_beatmap.OutputType} setid:{current_beatmap.BeatmapSetId} mod:{mod}");
        }

        private void OutputBackgroundImage(string bgPath, OutputType type)
        {
            if (!EnableOutputBackgroundImage.ToBool())
                return;

            if (!File.Exists(bgPath))
            {
                Log.Warn($"{IMAGE_NOT_FOUND}{bgPath}");
            }

            if (EnableListenOutputImageFile == "True" || type == OutputType.Play)
            {
                try
                {
                    if (EnableScaleClipOutputImageFile == "True")
                    {
                        using (Bitmap bitmap = GetFixedResolutionBitmap(bgPath, int.Parse(Width, CultureInfo.InvariantCulture), int.Parse(Height, CultureInfo.InvariantCulture)))
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
                    Log.Error($"{CANT_PROCESS_IMAGE}:{e.Message}");
                }
            }
        }

        #region tool func

        private void TryCreateModsPictureGenerator(out ModsPictureGenerator modsPictureGenerator)
        {
            Process process = Process.GetProcessesByName("osu!")?.First();
            if (process == null)
            {
                modsPictureGenerator = null;
                return;
            }

            string osu_path = Path.GetDirectoryName(process.MainModule.FileName);
            string osu_config_file = Path.Combine(osu_path, $"osu!.{Environment.UserName}.cfg");
            string using_skin_name = string.Empty;

            var lines = File.ReadLines(osu_config_file);
            foreach (var line in lines)
            {
                if (line.Trim().StartsWith("Skin =") || line.Trim().StartsWith("Skin="))
                {
                    using_skin_name = line.Split('=')[1].Trim();
                }
            }

            if (string.IsNullOrWhiteSpace(using_skin_name))
            {
                modsPictureGenerator = null;
                return;
            }

            string using_skin_path = Path.Combine(osu_path, "Skins", using_skin_name);

            Log.Output($"Enable to ouput mod pics , using_skin_path={using_skin_path}");

            modsPictureGenerator = new ModsPictureGenerator(using_skin_path, ModSkinPath, int.Parse(ModUnitPixel, CultureInfo.InvariantCulture), int.Parse(ModUnitOffset, CultureInfo.InvariantCulture), ModIsHorizon == "True", ModUse2x == "True", ModSortReverse == "True", ModDrawReverse == "True");
        }

        private bool OutputInfomation(OutputType output_type, BeatmapEntry entry, ModsInfo mods)
        {
            KeyValuePair<string, string>[] extra_Data_arr = new[]
            {
                new KeyValuePair<string, string>( "osu_file_path", entry.OsuFilePath ),
                new KeyValuePair<string, string>( "beatmap_id", entry.BeatmapId.ToString() ),
                new KeyValuePair<string, string>( "beatmap_setid", entry.BeatmapSetId.ToString() )
            };

            return PPShowPluginInstance.Output(output_type, entry.OsuFilePath, mods, extra_Data_arr);
        }

        private Bitmap GetFixedResolutionBitmap(string file, int dstw, int dsth)
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
                if (h > sbitmap.Height)
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
                    byte* sp_up, sp_down, sp_left, sp_right;
                    int si = 0, sj = 0;

                    float t;
                    float u, v, omu, omv;
                    Vector4 abcd;
                    Vector4 g1, g2, g3;

                    for (int i = 0; i < ddata.Height; i++, dptr += ddata.Stride - ddata.Width * 3)
                    {
                        t = i * scaley;
                        si = (int)(t);
                        v = t - si;

                        if ((si + 1) == sdata.Height) continue;

                        for (int j = 0; j < ddata.Width; j++, dptr += 3)
                        {
                            t = j * scalex;
                            sj = (int)(t);
                            u = t - sj;
                            if ((sj + 1) == sdata.Width) continue;

                            omu = 1 - u;
                            omv = 1 - v;

                            abcd.X = omu * omv; abcd.Y = u * v; abcd.Z = omu * v; abcd.W = omv * u;

                            sp_up = sptr + ((si - 0) * sdata.Stride + (sj - 0) * 3);//left up 0,0
                            sp_down = sptr + ((si + 1) * sdata.Stride + (sj + 1) * 3);//right down 1,1
                            sp_left = sptr + ((si + 1) * sdata.Stride + (sj - 0) * 3);//left down 0,1
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
                Log.Output($"线性插值:{stopwatch.ElapsedMilliseconds}ms");
            }
            return dbitmap;
        }

        #region DDPR

        private static readonly string[] ppshow_provideable_data_array = new[] { "ar", "cs", "od", "hp", "pp", "beatmap_setid", "version", "title_avaliable", "artist_avaliable", "beatmap_setlink", "beatmap_link", "beatmap_id", "min_bpm", "max_bpm", "speed_stars", "aim_stars", "stars", "mods", "title", "creator", "max_combo", "artist", "circles", "spinners","sliders" };

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

            if (GetCurrentPluginData(name, out string result))
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

        #endregion DDPR

        public void onConfigurationLoad()
        {
        }

        public void onConfigurationSave()
        {
        }

        public void onConfigurationReload()
        {
            mods_pic_output = null;
            if (source == UsingSource.OsuRTDataProvider)
            {
                TryCreateModsPictureGenerator(out mods_pic_output);
            }
            OnSettingChanged?.Invoke();
        }

        public override void OnExit()
        {
            base.OnExit();
            PPShowPluginInstance?.Exit();
            OutputInfomationClean();
        }

        #endregion tool func

        #endregion Kernal
    }
}