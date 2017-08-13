using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NowPlaying;
using System.Text.RegularExpressions;
using System.Drawing;
using osu_database_reader;
using OsuLiveStatusPanel;

namespace ConsoleApp1
{
    class Program
    {

        static void Main(string[] args)
        {
            var osu_process = Process.GetProcessesByName("osu!")?.First();

            if (osu_process!=null)
            {
                string CurrentOsuPath = osu_process.MainModule.FileName.Replace(@"osu!.exe", string.Empty);

                var list=Directory.EnumerateDirectories(CurrentOsuPath+"Songs", "448281 *");

                foreach (var path in Directory.EnumerateFiles(list.First()))
                {
                    if (!path.EndsWith(".osu"))
                    {
                        continue;
                    }
                    string content = File.ReadAllText(path);
                    var res = OsuFileParser.ParseText(content);

                    OsuLiveStatusPanelPlugin p = new OsuLiveStatusPanelPlugin();
                    p.Np_OnCurrentPlayingBeatmapChangedEvent(res);
                }
            }
        }
    }
}
