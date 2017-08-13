using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

using Sync;
using Sync.Plugins;
using Sync.Tools;
using NowPlaying;
using osu_database_reader;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace OsuLiveStatusPanel
{
    public class OsuLiveStatusPanelPlugin : Plugin
    {
        #region Options

        public ConfigurationElement Width = "1920";
        public ConfigurationElement Height = "1080";

        public ConfigurationElement LiveWidth = "1600";
        public ConfigurationElement LiveHeight = "900";

        public ConfigurationElement BlurRadius = "7";

        public ConfigurationElement FontSize = "15";

        public ConfigurationElement EnablePrintArtistTitle = "0";
        public ConfigurationElement EnableAutoStartPPShower = "1";
        public ConfigurationElement PPShowerFilePath = @"PPShowPlugin.exe";

        public ConfigurationElement OutputImageFilePath = @"e:\current_playing_panel.png";
        public ConfigurationElement OutputArtistTitleDiffFilePath = @"e:\current_playing_2.txt";
        public ConfigurationElement OutputOsuFilePath = @"in_current_playing.txt";
        public ConfigurationElement OutputBeatmapNameInfoFilePath = @"e:\current_playing_beatmap_info.txt";
        public ConfigurationElement OutputBackgroundImageFilePath = @"e:\result.png";
        #endregion

        GaussianBlur blur=null;

        Pen pen = new Pen(Color.FromArgb(170, 255, 255, 0), 25);

        SolidBrush Artist_TittleBrush = new SolidBrush(Color.Aqua);

        Task RunningTask;

        string OsuSyncPath;

        CancellationTokenSource token; 

        string CurrentOsuPath = "";

        public OsuLiveStatusPanelPlugin() : base("OsuLiveStatusPanelPlugin", "MikiraSora >///<")
        {
            onInitPlugin += OsuLiveStatusPanelPlugin_onInitPlugin;
            onLoadComplete += OsuLiveStatusPanelPlugin_onLoadComplete;
            onInitCommand += OsuLiveStatusPanelPlugin_onInitCommand;
        }

        private void OsuLiveStatusPanelPlugin_onInitCommand(Sync.Command.CommandManager commands)
        {
            commands.Dispatch.bind("livestatuspanel", (args)=> 
            {
                IO.CurrentIO.Write($"CurrentOsuPath = {CurrentOsuPath}");
                return true;
            }, "获取屙屎状态面板的数据");
        }

        private void OsuLiveStatusPanelPlugin_onLoadComplete(SyncHost host)
        {
            foreach (var plugin in host.EnumPluings())
            {
                if (plugin.Name=="Now Playing")
                {
                    IO.CurrentIO.WriteColor("[OsuLiveStatusPanelPlugin]Found NowPlaying Plugin.", ConsoleColor.Green);
                    NowPlaying.NowPlaying np = plugin as NowPlaying.NowPlaying;
                    np.OnCurrentPlayingBeatmapChangedEvent += Np_OnCurrentPlayingBeatmapChangedEvent;
                    return;
                }
            }
            IO.CurrentIO.WriteColor("[OsuLiveStatusPanelPlugin]NowPlaying Plugin is not found,Please check your plugins folder", ConsoleColor.Red);
        }

        public void Np_OnCurrentPlayingBeatmapChangedEvent(BeatmapEntry new_beatmap)
        {
            var osu_process = Process.GetProcessesByName("osu!")?.First();

            if (new_beatmap==null||osu_process==null)
            {
                if (osu_process == null)
                    IO.CurrentIO.WriteColor("[OsuLiveStatusPanelPlugin]osu program is not found!", ConsoleColor.Red);
                CleanOsuStatus();
                return;
            }

            if (token!=null)
            {
                token.Cancel();
            }

            token = new CancellationTokenSource();

            CurrentOsuPath = osu_process.MainModule.FileName.Replace(@"osu!.exe",string.Empty);
            Task task = new Task(new Action<object>(TryChangeOsuStatus),(object)new_beatmap,token.Token);
            task.Start();
        }

        private void CleanOsuStatus()
        {
            IO.CurrentIO.WriteColor("[OsuLiveStatusPanelPlugin]Clean Status", ConsoleColor.Green);

            File.WriteAllText(OutputOsuFilePath, string.Empty);

            File.WriteAllText(OutputArtistTitleDiffFilePath, $@"选图中 >///<");

            File.WriteAllText(OutputBeatmapNameInfoFilePath, string.Empty);

            if (File.Exists(OutputBackgroundImageFilePath))
            {
                File.Delete(OutputBackgroundImageFilePath);
            }
        }

        private void TryChangeOsuStatus(object obj)
        {
            BeatmapEntry beatmap = obj as BeatmapEntry;
            if (!ChangeOsuStatus(beatmap))
            {
                CleanOsuStatus();
            }
        }

        private void CheckPPShowPluginProgram()
        {
            if (EnableAutoStartPPShower == "1")
            {
                if (Process.GetProcessesByName("PPShowPlugin").Count() == 0)
                {
                    File.WriteAllText(OutputOsuFilePath, "");
                    IO.CurrentIO.WriteColor("[OsuLiveStatusPanelPlugin]PPShowPlugin is not running,will start this program.", ConsoleColor.Yellow);
                    Process.Start(new ProcessStartInfo(OsuSyncPath + PPShowerFilePath, "")
                    {
                        WorkingDirectory = OsuSyncPath,
                        CreateNoWindow = true
                    });
                }
            }
        }

        private bool ChangeOsuStatus(BeatmapEntry current_beatmap)
        {
            CheckPPShowPluginProgram();

            #region Create Bitmap

            Bitmap bitmap = new Bitmap(int.Parse(Width), int.Parse(Height));
            Graphics graphics = Graphics.FromImage(bitmap);

            Font font = new Font("Consolas",25);
            #endregion

            #region GetInfo

            string beatmap_folder = GetBeatmapFolderPath(current_beatmap.BeatmapSetId.ToString());
            string beatmap_osu_file = GetCurrentBeatmapOsuFilePath(current_beatmap.Difficulty, beatmap_folder);

            if (string.IsNullOrWhiteSpace(beatmap_osu_file))
            {
                IO.CurrentIO.WriteColor("[OsuLiveStatusPanelPlugin]Cant get current beatmap file path.", ConsoleColor.Red);
                return false;
            }

            string osuFileContent = File.ReadAllText(beatmap_osu_file);

            File.WriteAllText(OutputOsuFilePath, beatmap_osu_file);

            File.WriteAllText(OutputBeatmapNameInfoFilePath, $"Creator:{current_beatmap.Creator} \t \t Link:http://osu.ppy.sh/s/{current_beatmap.BeatmapSetId}");

            File.WriteAllText(OutputArtistTitleDiffFilePath, $@"CurrentPlaying : {GetArtist(current_beatmap)} - {GetTitle(current_beatmap)}[{current_beatmap.Difficulty ?? "<unknown diff>"}]");

            //var parse_data = OsuFileParser.PickValues(ref osuFileContent);
            string bgPath = beatmap_folder+@"/"+Regex.Match(osuFileContent, @"//Background[\w\s]+?\n\d+,\d+,\""(.+?)\""").Groups[1].Value;

            #endregion

            #region Draw Content

            //draw background image with blur etc.
            var bgImage = GetBeatmapBackgroundImage(bgPath);
            var blurImage = GetBlurImage(bgImage);
            bgImage.Dispose();
            graphics.DrawImage(blurImage, new PointF(0, 0));
            blurImage.Dispose();
            //draw bitmap data
            //graphics.DrawRectangle(pen, 0, 0, float.Parse(LiveWidth), float.Parse(LiveHeight));
            //draw artist - title[diff] (if enable)
            if (EnablePrintArtistTitle=="1")
            {
                graphics.DrawString($"Current Playing:{GetArtist(current_beatmap)} - {GetTitle(current_beatmap)}[{current_beatmap.Difficulty}]", font, Artist_TittleBrush, new RectangleF(new PointF(0, float.Parse(LiveHeight) + 40), new SizeF(float.Parse(LiveWidth), 60)));
            }

            #endregion

            //save
            graphics.Save();
            graphics.Dispose();
            try
            {
                bitmap.Save(OutputBackgroundImageFilePath, ImageFormat.Jpeg);
            }
            catch { }
            bitmap.Dispose();

            IO.CurrentIO.WriteColor($"[OsuLiveStatusPanelPlugin]Done! setid:{current_beatmap.BeatmapSetId}", ConsoleColor.Green);

            return true;
        }

        private string GetArtist(BeatmapEntry beatmap) => string.IsNullOrWhiteSpace(beatmap.ArtistUnicode) ? beatmap.Artist : beatmap.ArtistUnicode;

        private string GetTitle(BeatmapEntry beatmap) => string.IsNullOrWhiteSpace(beatmap.TitleUnicode) ? beatmap.Title : beatmap.TitleUnicode;

        private string GetBeatmapFolderPath(string beatmap_sid)
        {
            var query_result=Directory.EnumerateDirectories(CurrentOsuPath+"Songs",beatmap_sid+" *");

            if (query_result.Count()==0)
            {
                return string.Empty;
            }

            return query_result.First();
        }
        
        private string GetCurrentBeatmapOsuFilePath(string diff_name,string beatmapPath)
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

        private Bitmap GetBeatmapBackgroundImage(string bgFilePath)
        {
            Bitmap bitmap = new Bitmap(Image.FromFile(bgFilePath), new Size(int.Parse(Width), int.Parse(Height)));
            return bitmap;
        }

        private Bitmap GetBlurImage(Bitmap bitmap)
        {
            /*
            if (blur==null)
            {
                blur = new GaussianBlur(int.Parse(BlurRadius));
                blur.BlurType = BlurType.Both;
            }

            return blur.ProcessImage(bitmap);
            */
            GaussianBlur blur = new GaussianBlur(bitmap);
            return blur.Process(int.Parse(BlurRadius));
        }

        private void ApplyImage(Bitmap bitmap)
        {
            bitmap.Save(OutputImageFilePath);
        }
        
        private void OsuLiveStatusPanelPlugin_onInitPlugin()
        {
            Sync.Tools.IO.CurrentIO.WriteColor(this.Name+" by "+this.Author, System.ConsoleColor.DarkCyan);
            OsuSyncPath = Directory.GetParent(Environment.CurrentDirectory).FullName + @"\";
            CheckPPShowPluginProgram();
        }
    }
}
