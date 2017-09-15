using Sync.Plugins;
using Sync;
using Sync.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace OsuLiveStatusPanel
{
    public class PPShowPlugin /*: Plugin*/
    {
        public static ConfigurationElement PPShowJsonConfigFilePath { set; get; } = "PPShowConfig.json";

        public static ConfigurationElement PPShowAllowDumpInfo { get; set; } = "0";

        Dictionary<OutputConfig, OutputFormatter> ofs;

        PPCalculator PP;

        public PPShowPlugin(string config_path)/*(string Name, string Author) : base("PPShowPlugin", "Mikira Sora")*/
        {
            if (!File.Exists(PPShowJsonConfigFilePath))
            {
                Config.InitConfigFile(PPShowJsonConfigFilePath);
                throw new Exception("不存在指定路径的PPShowPlugin的配置文件.现在已经创建默认配置文件，请自行配置");
            }

            LoadConfig(config_path);
            Init();
        }

        private void LoadConfig(string config_path)
        {
            Config.InitConfig(config_path);
        }

        private void Init()
        {
            ofs = new Dictionary<OutputConfig, OutputFormatter>();

            foreach (var o in Config.Instance.output_list)
                ofs[o] = new OutputFormatter(o.output_format);

            List<float> acc_list = new List<float>();

            foreach (var o in ofs)
            {
                acc_list = acc_list.Concat(
                    from n in o.Value.GetAccuracyArray()
                    where !acc_list.Contains(n)
                    select n
                    ).ToList();
            }

            if (Config.Instance.input_file.Length == 0)
            {
                throw new Exception("无法解析指定的PPShow配置文件");
            }
            
            string oppai_path = Config.Instance.oppai;

            PP = new PPCalculator(oppai_path, acc_list);

            PP.OnOppainJson += OnOppaiJson;
            PP.OnBackMenu += OnBackMenu;
        }

        private void OnOppaiJson(List<OppaiJson> oppai_infos)
        {
            foreach (var of in ofs)
            {
                string str = of.Value.Format(oppai_infos);

                if (PPShowAllowDumpInfo == "1")
                {

                    IO.CurrentIO.WriteColor("[PPShow][", ConsoleColor.White, false);
                    IO.CurrentIO.WriteColor($"{of.Key.output_file}", ConsoleColor.Red, false);

                    IO.CurrentIO.WriteColor("]", ConsoleColor.White, false);
                    IO.CurrentIO.WriteColor($"{str}", ConsoleColor.White);
                }

                try
                {
                    File.WriteAllText(of.Key.output_file, str);
                }
                catch (Exception e)
                {
                    IO.CurrentIO.WriteColor($"无法写入{of.Key.output_file},原因{e.Message}", ConsoleColor.Red);
                }
            }
        }


        private void OnBackMenu()
        {
            foreach (var o in Config.Instance.output_list)
            {
                try
                {
                    File.WriteAllText($"{o.output_file}", "");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"无法写入{o.output_file},原因{e.Message}");
                }
            }
        }

        public void CalculateAndDump(string osu_file_path,string mods_list)
        {
            PP.TrigCalc(osu_file_path, mods_list);

        }
    }
}
