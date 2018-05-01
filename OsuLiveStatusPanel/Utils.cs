using Sync.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OsuLiveStatusPanel
{
    public static class Utils
    {
        public static List<float> GetAccuracyArray(string content)
        {
            List<float> result = new List<float>();

            var m = Regex.Match(content, @"\$\{pp:\d{1,3}\.\d{2}%\}");

            while (m.Success)
            {
                var acc_m = Regex.Match(m.Groups[0].Value, @"\d{1,3}\.\d{2}");
                float acc = float.Parse(acc_m.Groups[0].Value);
                result.Add(acc);

                m = m.NextMatch();
            }

            return result;
        }

        static Stopwatch sw=new Stopwatch();

        public static void RecordTime(string name,Action d)
        {
            sw.Reset();
            sw.Start();
            d();
            IO.CurrentIO.WriteColor($"{name} use time:{sw.ElapsedMilliseconds}", ConsoleColor.Magenta);
            sw.Stop();
        }
    }
}
