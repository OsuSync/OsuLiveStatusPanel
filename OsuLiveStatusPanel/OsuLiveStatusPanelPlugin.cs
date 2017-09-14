using MemoryReader.BeatmapInfo;
using MemoryReader.Mods;
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

        #region Options

        public ConfigurationElement AllowUsedMemoryReader { get; set; } = "0";
        public ConfigurationElement AllowUsedNowPlaying { get; set; } = "1";

        public ConfigurationElement AllowGetDiffNameFromOsuAPI { get; set; } = "1";

        public ConfigurationElement Width { get; set; } = "1920";
        public ConfigurationElement Height { get; set; } = "1080";

        public ConfigurationElement LiveWidth { get; set; } = "1600";
        public ConfigurationElement LiveHeight { get; set; } = "900";

        public ConfigurationElement BlurRadius { get; set; } = "7";

        public ConfigurationElement FontSize { get; set; } = "15";

        public ConfigurationElement EnablePrintArtistTitle { get; set; } = "0";
        public ConfigurationElement EnableAutoStartPPShowPlugin { get; set; } = "1";
        public ConfigurationElement PPShowPluginFilePath { get; set; } = @"PPShowPlugin.exe";

        /// <summary>
        /// 当前游戏谱面的信息文件保存路径(CurrentPlaying: Artist - Title[DiffName])
        /// </summary>
        public ConfigurationElement OutputArtistTitleDiffFilePath { get; set; } = @"output_current_playing.txt";

        /// <summary>
        /// 供PPShowPlugin使用的文件保存路径,必须和前者设置一样否则无效
        /// </summary>
        public ConfigurationElement OutputOsuFilePath { get; set; } = @"in_current_playing.txt";

        /// <summary>
        /// 当前游戏谱面的信息文件保存路径
        /// </summary>
        public ConfigurationElement OutputBeatmapNameInfoFilePath { get; set; } = @"output_current_playing_beatmap_info.txt";

        /// <summary>
        /// 当前谱面背景文件保存路径
        /// </summary>
        public ConfigurationElement OutputBackgroundImageFilePath { get; set; } = @"output_result.png";

        /// <summary>
        /// 当前游戏最佳本地成绩的信息文件保存路径
        /// </summary>
        public ConfigurationElement OutputBestLocalRecordInfoFilePath { get; set; } = @"output_best_local_record_info.txt";

        #endregion Options

        private UsingSource source = UsingSource.None;

        private Pen pen = new Pen(Color.FromArgb(170, 255, 255, 0), 25);

        private SolidBrush Artist_TittleBrush = new SolidBrush(Color.Aqua);

        private PluginConfigurationManager manager;

        private string OsuSyncPath;

        private CancellationTokenSource token;

        private string CurrentOsuPath = "";

        private PluginConfiuration config;

        #region MemoryReader
        public class MemoryReaderWrapper
        {
            OsuLiveStatusPanelPlugin RefPlugin;

            public ModsInfo current_mod;

            private int beatmapID, beatmapSetID;

            public MemoryReaderWrapper(OsuLiveStatusPanelPlugin p) => RefPlugin = p;

            public void OnCurrentBeatmapChange(Beatmap beatmap)
            {
                beatmapID = beatmap.BeatmapID;
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
                     RefPlugin.Np_OnCurrentPlayingBeatmapChangedEvent(null);
                }
                else
                {
                    //load
                    BeatmapEntry beatmap = new BeatmapEntry()
                    {
                        BeatmapId = beatmapID,
                        BeatmapSetId = beatmapSetID
                    };

                    RefPlugin.Np_OnCurrentPlayingBeatmapChangedEvent(new CurrentPlayingBeatmapChangedEvent(beatmap));
                }
            }
        }

        MemoryReaderWrapper MemoryReaderWrapperInstance;
        #endregion MemoryReader

        public OsuLiveStatusPanelPlugin() : base("OsuLiveStatusPanelPlugin", "MikiraSora >///<")
        {
            base.EventBus.BindEvent<PluginEvents.InitPluginEvent>(OsuLiveStatusPanelPlugin_onInitPlugin);
            base.EventBus.BindEvent<PluginEvents.LoadCompleteEvent>(OsuLiveStatusPanelPlugin_onLoadComplete);
            base.EventBus.BindEvent<PluginEvents.InitCommandEvent>(OsuLiveStatusPanelPlugin_onInitCommand);
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
                    EventBus.BindEvent<NowPlaying.CurrentPlayingBeatmapChangedEvent>(Np_OnCurrentPlayingBeatmapChangedEvent);

                    source = UsingSource.NowPlaying;

                    return;
                }
            }

            IO.CurrentIO.WriteColor("[OsuLiveStatusPanelPlugin]NowPlaying Plugin is not found,Please check your plugins folder", ConsoleColor.Red);

            source = UsingSource.None;
        }

        #region Kernal

        public void Np_OnCurrentPlayingBeatmapChangedEvent(CurrentPlayingBeatmapChangedEvent evt)
        {
            BeatmapEntry new_beatmap = evt?.NewBeatmap;

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

            File.WriteAllText(OutputOsuFilePath, string.Empty);

            File.WriteAllText(OutputArtistTitleDiffFilePath, $@"选图中 >///<");

            File.WriteAllText(OutputBeatmapNameInfoFilePath, string.Empty);

            File.WriteAllText(OutputBestLocalRecordInfoFilePath, string.Empty);

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

        private object locker = new object();

        private void CheckPPShowPluginProgram()
        {
            if (EnableAutoStartPPShowPlugin == "1")
            {
                lock (locker)
                {
                    if (Process.GetProcessesByName("PPShowPlugin").Count() == 0)
                    {
                        File.WriteAllText(OutputOsuFilePath, "");
                        IO.CurrentIO.WriteColor("[OsuLiveStatusPanelPlugin]PPShowPlugin is not running,will start this program.", ConsoleColor.Yellow);
                        Process.Start(new ProcessStartInfo(OsuSyncPath + PPShowPluginFilePath, "")
                        {
                            WorkingDirectory = OsuSyncPath,
                            CreateNoWindow = true
                        });
                    }
                }
            }
        }

        private bool ChangeOsuStatusforMemoryReader(BeatmapEntry current_beatmap)
        {
            CheckPPShowPluginProgram();

            string beatmap_folder = GetBeatmapFolderPath(current_beatmap.BeatmapSetId.ToString());

            string beatmap_osu_file = string.Empty;

            beatmap_osu_file = GetCurrentBeatmapOsuFilePathByBeatmapID(current_beatmap.BeatmapId.ToString(), beatmap_folder);
            if (string.IsNullOrWhiteSpace(beatmap_osu_file))
            {
                beatmap_osu_file = GetCurrentBeatmapOsuFilePathByAPI(current_beatmap.BeatmapId.ToString(), beatmap_folder);
            }

            if (string.IsNullOrWhiteSpace(beatmap_osu_file))
            {
                IO.CurrentIO.WriteColor("[OsuLiveStatusPanelPlugin]Cant get current beatmap file path.", ConsoleColor.Red);
                return false;
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

        private bool ChangeOsuStatusforNowPlaying(BeatmapEntry current_beatmap)
        {
            CheckPPShowPluginProgram();

            #region GetInfo

            string beatmap_folder = GetBeatmapFolderPath(current_beatmap.BeatmapSetId.ToString());

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

            #region Create Bitmap

            Bitmap bitmap = new Bitmap(int.Parse(Width), int.Parse(Height));
            Graphics graphics = Graphics.FromImage(bitmap);

            Font font = new Font("Consolas", 25);

            #endregion Create Bitmap

            File.WriteAllText(OutputOsuFilePath, beatmap_osu_file + $"@{mod}");

            File.WriteAllText(OutputBeatmapNameInfoFilePath, $"Creator:{current_beatmap.Creator} \t \t Link:http://osu.ppy.sh/s/{current_beatmap.BeatmapSetId}");

            File.WriteAllText(OutputArtistTitleDiffFilePath, $@"CurrentPlaying : {GetArtist(current_beatmap)} - {GetTitle(current_beatmap)}[{current_beatmap.Difficulty ?? "<unknown diff>"}]");

            //var parse_data = OsuFileParser.PickValues(ref osuFileContent);
            var match = Regex.Match(osuFileContent, @"\d,\d,\""((.+?)\.((jpg)|(png)))\""(,\d,\d)?");
            string bgPath = beatmap_folder + @"\" + match.Groups[1].Value;

            #endregion GetInfo

            #region Draw Content

            //draw background image with blur etc.
            var bgImage = GetBeatmapBackgroundImage(bgPath);
            if (bgImage != null)
            {
                var blurImage = GetBlurImage(bgImage);
                bgImage.Dispose();
                graphics.DrawImage(blurImage, new PointF(0, 0));
                blurImage.Dispose();
            }
            //draw bitmap data
            //graphics.DrawRectangle(pen, 0, 0, float.Parse(LiveWidth), float.Parse(LiveHeight));
            //draw artist - title[diff] (if enable)
            if (EnablePrintArtistTitle == "1")
            {
                graphics.DrawString($"Current Playing:{GetArtist(current_beatmap)} - {GetTitle(current_beatmap)}[{current_beatmap.Difficulty}]", font, Artist_TittleBrush, new RectangleF(new PointF(0, float.Parse(LiveHeight) + 40), new SizeF(float.Parse(LiveWidth), 60)));
            }

            #endregion Draw Content

            #region Save&Dispose

            //save
            graphics.Save();
            graphics.Dispose();
            try
            {
                bitmap.Save(OutputBackgroundImageFilePath, ImageFormat.Jpeg);
            }
            catch { }
            bitmap.Dispose();

            #endregion Save&Dispose

            IO.CurrentIO.WriteColor($"[OsuLiveStatusPanelPlugin]Done! setid:{current_beatmap.BeatmapSetId}", ConsoleColor.Green);
        }

        #region tool func

        private static double CalculateACC(int count_300, int count_100, int count_50, int count_miss)
        {
            double total = count_300
                + count_100 * ((double)1 / 3)
                + count_50 * ((double)1 / 6);

            double result = total / (count_50 + count_300 + count_100 + count_miss);

            return result;
        }

        private static string GetArtist(BeatmapEntry beatmap) => string.IsNullOrWhiteSpace(beatmap.ArtistUnicode) ? beatmap.Artist : beatmap.ArtistUnicode;

        private static string GetTitle(BeatmapEntry beatmap) => string.IsNullOrWhiteSpace(beatmap.TitleUnicode) ? beatmap.Title : beatmap.TitleUnicode;

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

        private void OsuLiveStatusPanelPlugin_onInitPlugin(PluginEvents.InitPluginEvent @event)
        {
            Sync.Tools.IO.CurrentIO.WriteColor(this.Name + " by " + this.Author, System.ConsoleColor.DarkCyan);

            manager = new PluginConfigurationManager(this);
            manager.AddItem(this);

            OsuSyncPath = Directory.GetParent(Environment.CurrentDirectory).FullName + @"\";
            CheckPPShowPluginProgram();
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