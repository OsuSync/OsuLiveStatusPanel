using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NowPlaying;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
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

                //"2d9cc0d498d23f185a9d3aed8864f1ab"
                //"(K)NoW_NAME - Morning Glory (kunka) [Easy].osu"
                Replay replay = null;

                Stopwatch sw = new Stopwatch();
                sw.Start();
                var hash = BeatmapHashHelper.GetHashFromOsuFile(@"g:\d.osu");
                Console.WriteLine($"hash={hash}");

                var db = osu_database_reader.ScoresDb.Read(CurrentOsuPath + "scores.db");

                var result=db.Beatmaps.AsParallel().Where((pair)=> 
                {
                    if (pair.Key==hash)
                    {
                        Console.WriteLine("找到一个");
                        return true;
                    }
                    return false;
                });

                if (result.Count() != 0)
                {
                    var list=result.First().Value;
                    list.Sort(
                        (a, b) => {
                            return a.Score-b.Score;
                        });

                    foreach (var item in list)
                    {
                        Console.WriteLine($"score:{item.Score} combo:{item.Combo}");
                    }

                    replay = list[0];
                }

                Console.WriteLine($"用时 {sw.ElapsedMilliseconds} ms,{(replay==null?"未":"")}找到记录!");
                sw.Stop();
            }
        }
    }
}
