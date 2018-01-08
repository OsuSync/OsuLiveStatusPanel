using Sync.Plugins;
using Sync;
using Sync.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using static OsuLiveStatusPanel.Languages;

namespace OsuLiveStatusPanel
{
    public class PPShowPlugin /*: Plugin*/
    {
        Dictionary<OutputConfig, OutputFormatter> ofs;

        PPCalculator PP;

        Dictionary<string, string> current_data_dic;

        public bool PPShowAllowDumpInfo = false;

        public Dictionary<string,string> CurrentOutputInfo { get => current_data_dic; }

        public PPShowPlugin(string config_path)/*(string Name, string Author) : base("PPShowPlugin", "Mikira Sora")*/
        {
            if (!File.Exists(config_path))
            {
                Config.InitConfigFile(config_path);
                throw new Exception(string.Format(PPSHOW_CONFIG_NOT_FOUND, config_path));
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
            {
                if(!Directory.Exists(Path.GetDirectoryName(o.output_file)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(o.output_file));
                }
                ofs[o] = new OutputFormatter(o.output_format);
            }

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
                throw new Exception(PPSHOW_CONFIG_PARSE_ERROR);
            }
            
            string oppai_path = Config.Instance.oppai;

            PP = new PPCalculator(oppai_path, acc_list);

            PP.OnOppainJson += OnOppaiJson;
            PP.OnBackMenu += OnBackMenu;
        }

        private void OnOppaiJson(List<OppaiJson> oppai_infos,Dictionary<string,string> data_dic)
        {
            current_data_dic = data_dic;

            foreach (var of in ofs)
            {
                string str = of.Value.Format(oppai_infos,data_dic);

                if (PPShowAllowDumpInfo == true)
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
                    IO.CurrentIO.WriteColor(string.Format(PPSHOW_IO_ERROR, of.Key.output_file, e.Message), ConsoleColor.Red);
                }
            }
        }


        private void OnBackMenu()
        {
            current_data_dic = null;

            foreach (var o in Config.Instance.output_list)
            {
                try
                {
                    File.WriteAllText($"{o.output_file}", "");
                }
                catch (Exception e)
                {
                    IO.CurrentIO.WriteColor(string.Format(PPSHOW_IO_ERROR, o.output_file, e.Message), ConsoleColor.Red);
                }
            }

            foreach (var o in Config.Instance.clean_list)
            {
                try
                {
                    File.WriteAllText($"{o.output_file}", o.output_format);
                }
                catch (Exception e)
                {
                    IO.CurrentIO.WriteColor(string.Format(PPSHOW_IO_ERROR, o.output_file, e.Message), ConsoleColor.Red);
                }
            }
        }

        public void CalculateAndDump(string osu_file_path, string mods_list)
        {
            PP.TrigCalc(osu_file_path, mods_list);
        }

        public void onConfigurationLoad()
        {

        }

        public void onConfigurationSave()
        {

        }
    }
}
