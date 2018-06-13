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
using OsuLiveStatusPanel.PPShow;
using OsuLiveStatusPanel.PPShow.Output;

namespace OsuLiveStatusPanel
{
    public class BeatmapInfomationGeneratorPlugin
    {
        struct OutputWrapper
        {
            public OutputFormatter formatter;
            public OutputBase outputter;
        }

        Dictionary<OutputConfig, OutputWrapper> ofs;

        BeatmapInfomationGenerator PP;

        Dictionary<string, string> current_data_dic;

        public bool PPShowAllowDumpInfo = false;

        public Dictionary<string,string> CurrentOutputInfo { get => current_data_dic; }

        public BeatmapInfomationGeneratorPlugin(string config_path)
        {
            if (!File.Exists(config_path))
            {
                Config.CreateDefaultPPShowConfig(config_path);
            }

            LoadConfig(config_path);
            Init();
        }

        private void LoadConfig(string config_path)
        {
            Config.LoadPPShowConfig(config_path);
        }

        private void Init()
        {
            ofs = new Dictionary<OutputConfig, OutputWrapper>();

            var register_list = new List<OutputConfig>();
            register_list.AddRange(Config.Instance.output_list);
            register_list.AddRange(Config.Instance.listen_list);

            foreach (var o in register_list)
            {
                ofs[o] = new OutputWrapper
                {
                    formatter = new OutputFormatter(o.output_format),
                    outputter = OutputBase.Create(o.output_file)
                };

                if (ofs[o].outputter is DiskFileOutput &&(!Directory.Exists(Path.GetDirectoryName(o.output_file))))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(o.output_file));
                }
            }

            List<float> acc_list = new List<float>();

            foreach (var o in ofs)
            {
                acc_list = acc_list.Concat(
                    from n in o.Value.formatter.GetAccuracyArray()
                    where !acc_list.Contains(n)
                    select n
                    ).ToList();
            }

            string oppai_path = Config.Instance.oppai;

            PP = new BeatmapInfomationGenerator(oppai_path, acc_list);

            PP.OnOutputEvent += OnOutput;
        }

        private void OnOutput(OutputType output_type,Dictionary<string,string> data_dic)
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
                    string str = of.formatter.Format(data_dic);

                    if (PPShowAllowDumpInfo == true)
                    {

                        IO.CurrentIO.WriteColor("[PPShow][", ConsoleColor.White, false);
                        IO.CurrentIO.WriteColor($"{output.output_file}", ConsoleColor.Red, false);

                        IO.CurrentIO.WriteColor("]", ConsoleColor.White, false);
                        IO.CurrentIO.WriteColor($"{str}", ConsoleColor.White);
                    }

                    try
                    {
                        of.outputter.Output(str);
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
                    var of = ofs[o];

                    if (!File.Exists(o.output_file))
                    {
                        continue;
                    }

                    of.outputter.Output(of.formatter.Format(null));
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
                    var of = ofs[o];
                    of.outputter.Output(of.formatter.Format(null));
                }
                catch (Exception e)
                {
                    IO.CurrentIO.WriteColor(string.Format(PPSHOW_IO_ERROR, o.output_file, e.Message), ConsoleColor.Red);
                }
            }
        }

        public bool Output(OutputType output_type,string osu_file_path, ModsInfo mods, params KeyValuePair<string, string>[] extra)
        {
            if (output_type==OutputType.Listen&&String.IsNullOrWhiteSpace(osu_file_path))
            {
                ListenClean();
                return true;
            }

            return PP.TrigOutput(output_type,osu_file_path, mods, extra);
        }

        #region DDRP

        public object GetData(string name)
        {
            if (CurrentOutputInfo == null)
                return null;

            if (name != "pp")
            {
                if (CurrentOutputInfo.TryGetValue(name, out string result))
                {
                    if (int.TryParse(result, out var ival))
                        return ival;
                    else if (double.TryParse(result, out var dval))
                        return dval;
                    return result;
                }
            }
            else
            {
                //output pp
                var list = new List<object>();
                foreach (var acc in PP.AccuracyList)
                    if (CurrentOutputInfo.TryGetValue($"pp:{acc:F2}%", out string pp))
                    {
                        if(double.TryParse(pp, out var dval))
                            list.Add(new { acc, pp=dval });
                    }

                return list;
            }

            return null;
        }

        #endregion
    }
}
