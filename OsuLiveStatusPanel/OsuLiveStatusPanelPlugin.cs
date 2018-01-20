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

        public ConfigurationElement AllowGetDiffNameFromOsuAPI { get; set; } = "1";

        public ConfigurationElement Width { get; set; } = "1920";
        public ConfigurationElement Height { get; set; } = "1080";

        public ConfigurationElement EnableGenerateNormalImageFile { get; set; } = "1";

        public ConfigurationElement EnableGenerateBlurImageFile { get; set; } = "0";
        public ConfigurationElement BlurRadius { get; set; } = "7";
        
        public ConfigurationElement PPShowJsonConfigFilePath { set; get; } = @"..\PPShowConfig.json";
        public ConfigurationElement PPShowAllowDumpInfo { get; set; } = "0"; 
        /// <summary>
        /// 供PPShowPlugin使用的文件保存路径,必须和前者设置一样否则无效
        /// </summary>
        public ConfigurationElement OutputOsuFilePath { get; set; } = @"..\in_current_playing.txt";
         
        /// <summary>
        /// 当前谱面背景文件保存路径
        /// </summary>
        public ConfigurationElement OutputBackgroundImageFilePath { get; set; } = @"..\output_result.png"; 

        public ConfigurationElement DebugOutputBGMatchFailedListFilePath{get;set;} =@"..\failed_list.txt";

        public ConfigurationElement EnableDebug{set;get;} =@"0";

        #endregion Options

        private UsingSource source = UsingSource.None;

        private PluginConfigurationManager manager;

        private string OsuSyncPath;

        private CancellationTokenSource token;
        private object locker = new object();

        public PPShowPlugin PPShowPluginInstance { get; private set; }

        private string CurrentOsuPath = "";
        
        //private PluginConfiuration config;

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
            //(re)load settings in config.ini
            manager?.GetInstance(this)?.ForceLoad();

            OsuSyncPath = Directory.GetParent(Environment.CurrentDirectory).FullName + @"\";

            //init PPShow
            PPShowPluginInstance = new PPShowPlugin(PPShowJsonConfigFilePath);

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

            if (token != null)
            {
                token.Cancel();
            }

            token = new CancellationTokenSource();

            CurrentOsuPath = osu_process.MainModule.FileName.Replace(@"osu!.exe", string.Empty);
            Task task = new Task(new Action<object>(TryChangeOsuStatus), (object)new_beatmap, token.Token);
            task.Start();
        }

        private void CleanOsuStatus()
        {
            IO.CurrentIO.WriteColor($"[OsuLiveStatusPanelPlugin]{CLEAN_STATUS}", ConsoleColor.Green);

            CleanPPShow(); 

            if (File.Exists(OutputBackgroundImageFilePath))
            {
                try
                {
                    File.Delete(OutputBackgroundImageFilePath);
                }
                catch { }
            }
        }

        private void TryChangeOsuStatus(object obj)
        {
            BeatmapEntry beatmap = obj as BeatmapEntry;
            if (!(source == UsingSource.OsuRTDataProvider ? ChangeOsuStatusforOsuRTDataProvider(beatmap) : ChangeOsuStatusforNowPlaying(beatmap)))
            {
                CleanOsuStatus();
            }
        }

        private bool ChangeOsuStatusforOsuRTDataProvider(BeatmapEntry current_beatmap)
        {
            OsuRTDataProviderWrapper OsuRTDataProviderWrapperInstance = SourceWrapper as OsuRTDataProviderWrapper;

            string beatmap_folder = GetBeatmapFolderPath(current_beatmap.BeatmapSetId.ToString());

            string beatmap_osu_file = current_beatmap.OsuFilePath;

            if (string.IsNullOrWhiteSpace(beatmap_osu_file))
            {
                beatmap_osu_file = GetCurrentBeatmapOsuFilePathByBeatmapID(current_beatmap.BeatmapId.ToString(), beatmap_folder);
                
                if (string.IsNullOrWhiteSpace(beatmap_osu_file))
                {
                    beatmap_osu_file = GetCurrentBeatmapOsuFilePathByAPI(current_beatmap.BeatmapId.ToString(), beatmap_folder);

                    if (string.IsNullOrWhiteSpace(beatmap_osu_file))
                    {
                        IO.CurrentIO.WriteColor($"[OsuLiveStatusPanelPlugin]{NO_BEATMAP_PATH}", ConsoleColor.Red);
                        return false;
                    }
                }
            }

            string osuFileContent = File.ReadAllText(beatmap_osu_file);

            int beatmapId = current_beatmap.BeatmapId, beatmapSetId = current_beatmap.BeatmapSetId;
            //补完beatmap必需内容
            /*current_beatmap = OsuFileParser.ParseText(osuFileContent);*/

            current_beatmap.BeatmapId = current_beatmap.BeatmapId == -1 ? beatmapId : current_beatmap.BeatmapId;
            current_beatmap.BeatmapSetId = current_beatmap.BeatmapSetId == -1 ? beatmapSetId : current_beatmap.BeatmapSetId;
            current_beatmap.OsuFilePath = beatmap_osu_file;

            string mod = string.Empty;
            //添加Mods
            if (OsuRTDataProviderWrapperInstance.current_mod.Mod != OsuRTDataProvider.Mods.ModsInfo.Mods.Unknown)
            {
                //处理不能用的PP
                mod = $"{OsuRTDataProviderWrapperInstance.current_mod.ShortName}";
            }

            OuputContent(current_beatmap, mod);

            return true;
        }

        public void InitBuildInPPShow()
        {

        }

        private void CleanPPShow()
        {
            PPShowPluginInstance.CalculateAndDump(string.Empty, string.Empty);
        }

        private bool ChangeOsuStatusforNowPlaying(BeatmapEntry current_beatmap)
        {
            #region GetInfo

            //string beatmap_folder = GetBeatmapFolderPath(current_beatmap.BeatmapSetId.ToString());

            string beatmap_osu_file = string.Empty;

            beatmap_osu_file = /*GetCurrentBeatmapOsuFilePathByDiffName(current_beatmap.Difficulty, beatmap_folder)*/current_beatmap.OsuFilePath;

            if (string.IsNullOrWhiteSpace(beatmap_osu_file))
            {
                IO.CurrentIO.WriteColor($"[OsuLiveStatusPanelPlugin]{NO_BEATMAP_PATH}", ConsoleColor.Red);
                return false;
            }

            OuputContent(current_beatmap);

            return true;
        }

        public void OuputContent(BeatmapEntry current_beatmap, string mod = "")
        {
            string beatmap_osu_file = current_beatmap.OsuFilePath;
            string osuFileContent = File.ReadAllText(beatmap_osu_file);
            string beatmap_folder = Directory.GetParent(beatmap_osu_file).FullName;
            
            OutputInfomation(beatmap_osu_file, mod); 
            
            var match = Regex.Match(osuFileContent, @"\""((.+?)\.((jpg)|(png)))\""",RegexOptions.IgnoreCase);
            string bgPath = beatmap_folder + @"\" + match.Groups[1].Value;

            if (!File.Exists(bgPath)&&EnableDebug=="1")
            {
                IO.CurrentIO.WriteColor($"[OsuLiveStatusPanelPlugin::OutputImage]{IMAGE_NOT_FOUND}{bgPath}", ConsoleColor.Yellow);

                try
                {
                    File.AppendAllText(DebugOutputBGMatchFailedListFilePath, $"[({DateTime.Now.ToShortDateString()}){DateTime.Now.ToShortTimeString()}]{beatmap_osu_file}{Environment.NewLine}");
                }
                catch { }
            }

            if (File.Exists(OutputBackgroundImageFilePath))
            {
                File.Delete(OutputBackgroundImageFilePath);
            }

            if (EnableGenerateNormalImageFile == "1")
            {
                try
                {
                    File.Copy(bgPath,OutputBackgroundImageFilePath);
                }
                catch (Exception e)
                {
                    IO.CurrentIO.WriteColor($"[OsuLiveStatusPanelPlugin]{CANT_PROCESS_IMAGE}:{e.Message}", ConsoleColor.Red);
                }
            }
            else if (EnableGenerateBlurImageFile == "1")
            {
                OutputBlurImage(bgPath);
            }
                
            #endregion GetInfo
            
            IO.CurrentIO.WriteColor($"[OsuLiveStatusPanelPlugin]Done! setid:{current_beatmap.BeatmapSetId} mod:{mod}", ConsoleColor.Green);
        }

        #region tool func

        private void OutputBlurImage(string bgPath)
        {
            if (!File.Exists(bgPath))
            {
                IO.CurrentIO.WriteColor($"[OsuLiveStatusPanelPlugin]{IMAGE_NOT_FOUND}:{bgPath}", ConsoleColor.Red);
                return;
            }

            //draw background image with blur etc.
            using (var bgImage = GetBeatmapBackgroundImage(bgPath))
            {
                if (bgImage != null)
                {
                    using (var blurImage = GetBlurImage(bgImage))
                    {
                        try
                        {
                            blurImage.Save(OutputBackgroundImageFilePath);
                        }catch(ExternalException e)
                        {
                            if (e.Message.Trim().ToUpper().StartsWith("GDI"))
                            {
                                Thread.Sleep(1000);
                                OutputBlurImage(bgPath);
                            }
                        }
                    }
                }
            }
        }

        private void OutputInfomation(string osu_file_path,string mod_list)
        {
            PPShowPluginInstance.CalculateAndDump(osu_file_path, mod_list);
        }

        private string GetBeatmapFolderPath(string beatmap_sid)
        {
            var query_result = Directory.EnumerateDirectories(CurrentOsuPath + "Songs", beatmap_sid + " *");

            if (query_result.Count() == 0)
            {
                return string.Empty;
            }

            return query_result.First();
        }

        private string GetCurrentBeatmapOsuFilePathByDiffName(string diff_name, string beatmapPath)
        {
            if (string.IsNullOrWhiteSpace(beatmapPath))
            {
                return string.Empty;
            }

            var query_list = Directory.EnumerateFiles(beatmapPath);
            foreach (var path in query_list)
            {
                if (path.Contains($"[{diff_name}].osu"))
                    return path;
            }

            return string.Empty;
        }

        private string GetCurrentBeatmapOsuFilePathByBeatmapID(string beatmapID, string beatmapPath)
        {
            if (string.IsNullOrWhiteSpace(beatmapPath))
            {
                return string.Empty;
            }

            var query_list = Directory.EnumerateFiles(beatmapPath, "*.osu");

            string check_str = $"BeatmapID:{beatmapID}";

            var query_result = query_list.AsParallel().Where((path) =>
            {
                using (StreamReader reader = new StreamReader(File.OpenRead(path)))
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();

                        if (line == check_str)
                        {
                            return true;
                        }

                        if (line == "[Difficulty]")
                            break;
                    }
                }

                return false;
            });

            return query_result.Count() == 0 ? string.Empty : query_result.First();
        }

        private Bitmap GetBeatmapBackgroundImage(string bgFilePath)
        {
            Image rawbitmap = null;

            try
            {
                rawbitmap = Bitmap.FromFile(bgFilePath);
                Bitmap bitmap = new Bitmap(rawbitmap, new System.Drawing.Size(int.Parse(Width), int.Parse(Height)));
                return bitmap;
            }
            catch
            {
                return null;
            }
            finally
            {
                rawbitmap?.Dispose();
            }
        }

        private Bitmap GetBlurImage(Bitmap bitmap)
        {
            GaussianBlur blur = new GaussianBlur(bitmap);
            return blur.Process(int.Parse(BlurRadius));
        }

        private string GetCurrentBeatmapOsuFilePathByAPI(string beatmapID, string folder_path)
        {
            string uri = @"https://osu.ppy.sh/api/get_beatmaps?" +
                $@"k=b9f8ca3fc035078a5b111380bc21cd0b8e79d7b5&b={beatmapID}&limit=1";

            HttpWebRequest request = HttpWebRequest.Create(uri) as HttpWebRequest;
            request.Method = "GET";

            try
            {
                var response = (HttpWebResponse)request.GetResponse();
                var stream = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                string data = stream.ReadToEnd();
                string diff_name = GetJSONValue(ref data, "version"); //diffName

                return GetCurrentBeatmapOsuFilePathByDiffName(diff_name.Trim(), folder_path);
            }
            catch
            {
                return string.Empty;
            }
        }

        private string GetJSONValue(ref string text, string key)
        {
            var result = Regex.Match(text, $"{key}\":\"(.+?)\"");

            if (!result.Success)
                return null;

            return result.Groups[1].Value;
        }

        public void onConfigurationLoad()
        {
        }

        public void onConfigurationSave()
        {
        }

        #endregion tool func

        #endregion Kernal
    }
}