using Sync.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsuLiveStatusPanel
{
    public class Log
    {
        static readonly Logger logger = new Logger("OsuLiveStatusPanel");

        public static void Output(string message) => logger.LogInfomation(message);
        public static void Error(string message) => logger.LogError(message);
        public static void Warn(string message) => logger.LogWarning(message);
    }
}
