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
    public class BeatmapInfomationGeneratorPlugin
    {
        Dictionary<OutputConfig, OutputFormatter> ofs;

        BeatmapInfomationGenerator PP;

        Dictionary<string, string> current_data_dic;

        public bool PPShowAllowDumpInfo = false;

        public Dictionary<string,string> CurrentOutputInfo { get => current_data_dic; }

        public BeatmapInfomationGeneratorPlugin(string config_path)
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

            var register_list = new List<OutputConfig>();
            register_list.AddRange(Config.Instance.output_list);
            register_list.AddRange(Config.Instance.listen_list);

            foreach (var o in register_list)
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

            PP = new BeatmapInfomationGenerator(oppai_path, acc_list);

            PP.OnOutputEvent += OnOutput;
        }

        private void OnOutput(OutputType output_type,List<OppaiJson> oppai_infos,Dictionary<string,string> data_dic)
        {
            current_data_dic = data_dic;
            
            CleanFileList(Config.Instance.output_list);
            CleanFileList(Config.Instance.listen_list);

            switch (output_type)
            {
                case OutputType.Listen:
                    _OutputFiles(Config.Instance.listen_list);
                    break;
                case OutputType.Play:
                    _OutputFiles(Config.Instance.output_list);
                    break;
                default:
                    break;
            }
            
            void _OutputFiles(List<OutputConfig> list)
            {
                foreach (var output in list)
                {
                    var of = ofs[output];
                    string str = of.Format(oppai_infos, data_dic);

                    if (PPShowAllowDumpInfo == true)
                    {

                        IO.CurrentIO.WriteColor("[PPShow][", ConsoleColor.White, false);
                        IO.CurrentIO.WriteColor($"{output.output_file}", ConsoleColor.Red, false);

                        IO.CurrentIO.WriteColor("]", ConsoleColor.White, false);
                        IO.CurrentIO.WriteColor($"{str}", ConsoleColor.White);
                    }

                    try
                    {
                        File.WriteAllText(output.output_file, str);
                    }
                    catch (Exception e)
                    {
                        IO.CurrentIO.WriteColor(string.Format(PPSHOW_IO_ERROR, output.output_file, e.Message), ConsoleColor.Red);
                    }
                }
            }
        }

        private void CleanFileList(List<OutputConfig> list)
        {
            foreach (var o in list)
            {
                try
                {
                    if (!File.Exists(o.output_file))
                    {
                        continue;
                    }

                    File.WriteAllText($"{o.output_file}",string.Empty);
                }
                catch (Exception e)
                {
                    IO.CurrentIO.WriteColor(string.Format(PPSHOW_IO_ERROR, o.output_file, e.Message), ConsoleColor.Red);
                }
            }
        }

        private void ListenClean()
        {
            current_data_dic = null;

            CleanFileList(Config.Instance.output_list);

            foreach (var o in Config.Instance.listen_list)
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

        public void Output(OutputType output_type,string osu_file_path, string mods_list)
        {
            if (output_type==OutputType.Listen&&String.IsNullOrWhiteSpace(osu_file_path))
            {
                ListenClean();
                return;
            }

            PP.TrigOutput(output_type,osu_file_path, mods_list);
        }
    }
}
