using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using Sync.Tools;

namespace OsuLiveStatusPanel
{
    public static class UpdateCheck
    {
        class Result
        {
            public string tag_name = "0.0.1";
        }

        public static void Check()
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.CreateDefault(new Uri(@"https://api.github.com/repos/MikiraSora/OsuLiveStatusPanel/releases/latest"));

            request.UserAgent = "OsuLiveStatusPanel_UpdateCheck";

            Task.Run(() =>
            {
                try
                {
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        string json = reader.ReadToEnd();

                        var parse_result = JsonConvert.DeserializeObject<Result>(json);

                        var parse_version = new Version(parse_result.tag_name.Trim('v', ' '));

                        var cur_version = Assembly.GetExecutingAssembly().GetName().Version;

                        if (parse_version > cur_version)
                        {
                                //新的
                                if (MessageBox.Show(null, $"检查到OsuLiveStatusPanel新版本,是否前往下载页面?\n当前版本:{cur_version.ToString(3)}\n新的版本:{parse_version.ToString(3)}", "Meow~", MessageBoxButtons.OKCancel) == DialogResult.OK)
                            {
                                Process.Start(@"https://github.com/MikiraSora/OsuLiveStatusPanel/releases");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    IO.CurrentIO.WriteColor($"[UpdateCheck]无法获取OsuLiveStatusPanel的更新信息,原因:{e.Message}", ConsoleColor.Yellow);
                }
            });
        }
    }
}
