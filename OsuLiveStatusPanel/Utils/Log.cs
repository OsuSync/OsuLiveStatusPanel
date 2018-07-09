using Sync.Tools;

namespace OsuLiveStatusPanel
{
    public class Log
    {
        private static readonly Logger logger = new Logger("OsuLiveStatusPanel");

        public static void Output(string message) => logger.LogInfomation(message);

        public static void Error(string message) => logger.LogError(message);

        public static void Warn(string message) => logger.LogWarning(message);
    }
}