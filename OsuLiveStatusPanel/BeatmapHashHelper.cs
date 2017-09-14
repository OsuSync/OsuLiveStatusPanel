using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace OsuLiveStatusPanel
{
    public static class BeatmapHashHelper
    {
        private static MD5 md5 = new MD5CryptoServiceProvider();

        public static string GetHashFromOsuFile(string osuFilePath)
        {
            StringBuilder sb = new StringBuilder();

            byte[] data = null;

            try
            {
                data = md5.ComputeHash(File.ReadAllBytes(osuFilePath));
            }
            catch { return string.Empty; }

            foreach (byte b in data)
                sb.Append(b.ToString("x2"));

            var result = sb.ToString();

            return result;
        }
    }
}