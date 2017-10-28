using MemoryReader.BeatmapInfo;
using MemoryReader.Mods;
using MemoryReader;
using NowPlaying;
using Sync;
using Sync.Plugins;
using Sync.Tools;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static MemoryReader.Listen.OSUListenerManager;

namespace OsuLiveStatusPanel
{
    public class OsuLiveStatusPanelPlugin : Plugin, IConfigurable
    {
        private enum UsingSource
        {
            MemoryReader,
            NowPlaying,
            None
        }

        #region MemoryReader
        public class MemoryReaderWrapper
        {
            OsuLiveStatusPanelPlugin RefPlugin;

            public ModsInfo current_mod;

            private int beatmapID, beatmapSetID;

            public string OsuFilePath;

            public MemoryReaderWrapper(OsuLiveStatusPanelPlugin p) => RefPlugin = p;

            public void OnCurrentBeatmapChange(Beatmap beatmap)
            {
                beatmapID = beatmap.BeatmapID;
                OsuFilePath = beatmap.LocationFile;
            }

            public void OnCurrentBeatmapSetChange(BeatmapSet beatmap)
            {
                beatmapSetID = beatmap.BeatmapSetID;
            }

            public void OnCurrentModsChange(ModsInfo mod)
            {
                current_mod = mod;
                IO.CurrentIO.WriteColor($"mod change : {mod.ShortName}", ConsoleColor.Blue);
            }

            public void OnStatusChange(OsuStatus last_status, OsuStatus status)
            {
                if (last_status == status) return;
                if (status != OsuStatus.Playing)
                {
                    RefPlugin.OnBeatmapChanged(new CurrentPlayingBeatmapChangedEvent(null));
                }
                else
                {
                    //load
                    BeatmapEntry beatmap = new BeatmapEntry()
                    {
                        BeatmapId = beatmapID,
                        BeatmapSetId = beatmapSetID,
                        OsuFilePath = OsuFilePath
                    };

                    RefPlugin.OnBeatmapChanged(new CurrentPlayingBeatmapChangedEvent(beatmap));
                }
            }
        }

        MemoryReaderWrapper MemoryReaderWrapperInstance;
        #endregion MemoryReader

        #region Options

        public ConfigurationElement AllowUsedMemoryReader { get; set; } = "0";
        public ConfigurationElement AllowUsedNowPlaying { get; set; } = "1";

        public ConfigurationElement AllowGetDiffNameFromOsuAPI { get; set; } = "1";

        public ConfigurationElement Width { get; set; } = "1920";
        public ConfigurationElement Height { get; set; } = "1080";
         
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
        PPShowPlugin PPShowPluginInstance;

        private string CurrentOsuPath = "";
        
        private PluginConfiuration config;

        public OsuLiveStatusPanelPlugin() : base("OsuLiveStatusPanelPlugin", "MikiraSora & KedamavOvO >///<")
        {
            
        }

        public override void OnEnable()
        {
            base.EventBus.BindEvent<PluginEvents.LoadCompleteEvent>(OsuLiveStatusPanelPlugin_onLoadComplete);
            base.EventBus.BindEvent<PluginEvents.InitCommandEvent>(OsuLiveStatusPanelPlugin_onInitCommand);

            manager = new PluginConfigurationManager(this);
            manager.AddItem(this);

            OsuSyncPath = Directory.GetParent(Environment.CurrentDirectory).FullName + @"\";

            CheckPPShowPlugin();

            Sync.Tools.IO.CurrentIO.WriteColor(this.Name + " by " + this.Author, System.ConsoleColor.DarkCyan);
        }

        private void OsuLiveStatusPanelPlugin_onInitCommand(PluginEvents.InitCommandEvent @event)
        {
            @event.Commands.Dispatch.bind("livestatuspanel", (args) =>
            {
                IO.CurrentIO.Write($"CurrentOsuPath = {CurrentOsuPath}");
                return true;
            }, "获取屙屎状态面板的数据");
        }

        private void OsuLiveStatusPanelPlugin_onLoadComplete(PluginEvents.LoadCompleteEvent evt)
        {
            SyncHost host = evt.Host;

            UpdateCheck.Check();

            try
            {
                if (((string)AllowUsedNowPlaying).Trim() == "1")
                {
                    TryRegisterSourceFromNowPlaying(host);
                }
                else if (((string)AllowUsedMemoryReader).Trim() == "1")
                {
                    TryRegisterSourceFromMemoryReader(host);
                }
            }
            catch (Exception)
            {
                source = UsingSource.None;
            }
        }

        public void TryRegisterSourceFromMemoryReader(SyncHost host)
        {
            foreach (var plugin in host.EnumPluings())
            {
                if (plugin.Name == "MemoryReader")
                {
                    IO.CurrentIO.WriteColor("[OsuLiveStatusPanelPlugin]Found MemoryReader Plugin.", ConsoleColor.Green);
                    MemoryReader.MemoryReader reader = plugin as MemoryReader.MemoryReader;

                    MemoryReaderWrapperInstance = new MemoryReaderWrapper(this);

                    reader.ListenerManager.OnBeatmapChanged += MemoryReaderWrapperInstance.OnCurrentBeatmapChange;
                    reader.ListenerManager.OnBeatmapSetChanged += MemoryReaderWrapperInstance.OnCurrentBeatmapSetChange;
                    reader.ListenerManager.OnStatusChanged += MemoryReaderWrapperInstance.OnStatusChange;
                    reader.ListenerManager.OnCurrentMods += MemoryReaderWrapperInstance.OnCurrentModsChange;

                    source = UsingSource.MemoryReader;

                    return;
                }
            }

            IO.CurrentIO.WriteColor("[OsuLiveStatusPanelPlugin]MemoryReader Plugin is not found,Please check your plugins folder", ConsoleColor.Red);

            source = UsingSource.None;
        }

        public void TryRegisterSourceFromNowPlaying(SyncHost host)
        {
            foreach (var plugin in host.EnumPluings())
            {
                if (plugin.Name == "Now Playing")
                {
                    IO.CurrentIO.WriteColor("[OsuLiveStatusPanelPlugin]Found NowPlaying Plugin.", ConsoleColor.Green);
                    NowPlaying.NowPlaying np = plugin as NowPlaying.NowPlaying;
                    NowPlayingEvents.Instance.BindEvent<NowPlaying.CurrentPlayingBeatmapChangedEvent>(OnBeatmapChanged);

                    source = UsingSource.NowPlaying;

                    return;
                }
            }

            IO.CurrentIO.WriteColor("[OsuLiveStatusPanelPlugin]NowPlaying Plugin is not found,Please check your plugins folder", ConsoleColor.Red);

            source = UsingSource.None;
        }

        #region Kernal

        public void OnBeatmapChanged(CurrentPlayingBeatmapChangedEvent evt)
        {
            BeatmapEntry new_beatmap = evt.NewBeatmap;

            var osu_process = Process.GetProcessesByName("osu!")?.First();

            if (new_beatmap == null || osu_process == null)
            {
                if (osu_process == null)
                    IO.CurrentIO.WriteColor("[OsuLiveStatusPanelPlugin]osu program is not found!", ConsoleColor.Red);
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
            IO.CurrentIO.WriteColor("[OsuLiveStatusPanelPlugin]Clean Status", ConsoleColor.Green);

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
            if (!(source == UsingSource.MemoryReader ? ChangeOsuStatusforMemoryReader(beatmap) : ChangeOsuStatusforNowPlaying(beatmap)))
            {
                CleanOsuStatus();
            }
        }
        private void CheckPPShowPlugin()
        {
            if (PPShowPluginInstance == null)
            {
                PPShowPluginInstance = new PPShowPlugin(PPShowJsonConfigFilePath);
            }
        }

        private bool ChangeOsuStatusforMemoryReader(BeatmapEntry current_beatmap)
        {
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
                        IO.CurrentIO.WriteColor("[OsuLiveStatusPanelPlugin]Cant get current beatmap file path.", ConsoleColor.Red);
                        return false;
                    }
                }
            }

            string osuFileContent = File.ReadAllText(beatmap_osu_file);

            int beatmapId = current_beatmap.BeatmapId, beatmapSetId = current_beatmap.BeatmapSetId;
            //补完beatmap必需内容
            current_beatmap = OsuFileParser.ParseText(osuFileContent);

            current_beatmap.BeatmapId = current_beatmap.BeatmapId == -1 ? beatmapId : current_beatmap.BeatmapId;
            current_beatmap.BeatmapSetId = current_beatmap.BeatmapSetId == -1 ? beatmapSetId : current_beatmap.BeatmapSetId;
            current_beatmap.OsuFilePath = beatmap_osu_file;

            string mod = string.Empty;
            //添加Mods
            if (MemoryReaderWrapperInstance.current_mod != null)
            {
                mod = $"{MemoryReaderWrapperInstance.current_mod.ShortName}";
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
                IO.CurrentIO.WriteColor("[OsuLiveStatusPanelPlugin]Cant get current beatmap file path.", ConsoleColor.Red);
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
            
            var match = Regex.Match(osuFileContent, @"\""((.+?)\.((jpg)|(png)))\""");
            string bgPath = beatmap_folder + @"\" + match.Groups[1].Value;

            if (!File.Exists(bgPath)&&EnableDebug=="1")
            {
                IO.CurrentIO.WriteColor($"[OsuLiveStatusPanelPlugin::OutputBlurImage]BG Files Not Exsit:{bgPath}", ConsoleColor.Yellow);

                try
                {
                    File.AppendAllText(DebugOutputBGMatchFailedListFilePath, $"[({DateTime.Now.ToShortDateString()}){DateTime.Now.ToShortTimeString()}]{beatmap_osu_file}\n");
                }
                catch { }
            }
            
            if (EnableGenerateBlurImageFile == "1")
            {
                OutputBlurImage(bgPath);
            }
                
            #endregion GetInfo
            
            IO.CurrentIO.WriteColor($"[OsuLiveStatusPanelPlugin]Done! setid:{current_beatmap.BeatmapSetId}", ConsoleColor.Green);
        }

        #region tool func

        private void OutputBlurImage(string bgPath)
        {
            if (!File.Exists(bgPath))
            {
                return;
            }

            //draw background image with blur etc.
            using (var bgImage = GetBeatmapBackgroundImage(bgPath))
            {
                if (bgImage != null)
                {
                    using (var blurImage = GetBlurImage(bgImage))
                    {
                        blurImage.Save(OutputBackgroundImageFilePath);
                    }
                }
            }
        }

        private void OutputInfomation(string osu_file_path,string mod_list)
        {
            CheckPPShowPlugin();

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
                Bitmap bitmap = new Bitmap(rawbitmap, new Size(int.Parse(Width), int.Parse(Height)));
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