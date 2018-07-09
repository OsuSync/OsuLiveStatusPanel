using Newtonsoft.Json;
using OsuLiveStatusPanel.Mods;
using OsuLiveStatusPanel.PPShow.Output;
using Sync.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using static OsuLiveStatusPanel.Languages;

namespace OsuLiveStatusPanel.PPShow
{
    public class InfoOutputterWrapper
    {
        private string m_config_path;

        public class OutputWrapper
        {
            public OutputFormatter formatter { get; set; }
            public OutputBase outputter { get; set; }
        }

        public List<OutputWrapper> ListenOfs { get; private set; }
        public List<OutputWrapper> PlayOfs { get; private set; }

        private InfoOutputter PP;

        public bool PPShowAllowDumpInfo = false;

        public Dictionary<string, string> CurrentOutputInfo { get; private set; }

        public InfoOutputterWrapper(string config_path)
        {
            m_config_path = config_path;

            if (!File.Exists(config_path))
            {
                Log.Warn($"Not found PPShowConfig.json or others config file for initilizing Outputter.OLSP will create a default PPShowConfig.json to {config_path}");
                Config.CreateDefaultPPShowConfig(config_path);
            }

            Init(config_path);
        }

        private void Init(string config_path)
        {
            var config_instance = Config.LoadPPShowConfig(config_path);
            ListenOfs = new List<OutputWrapper>();
            PlayOfs = new List<OutputWrapper>();

            foreach (var o in config_instance.output_list)
            {
                PlayOfs.Add(new OutputWrapper
                {
                    formatter = new OutputFormatter(o.output_format),
                    outputter = OutputBase.Create(o.output_file)
                });
            }

            foreach (var o in config_instance.listen_list)
            {
                ListenOfs.Add(new OutputWrapper
                {
                    formatter = new OutputFormatter(o.output_format),
                    outputter = OutputBase.Create(o.output_file)
                });
            }

            List<float> acc_list = new List<float>();

            foreach (var o in ListenOfs)
            {
                acc_list = acc_list.Concat(
                    from n in o.formatter.GetAccuracyArray()
                    where !acc_list.Contains(n)
                    select n
                    ).ToList();
            }

            foreach (var o in PlayOfs)
            {
                acc_list = acc_list.Concat(
                    from n in o.formatter.GetAccuracyArray()
                    where !acc_list.Contains(n)
                    select n
                    ).ToList();
            }

            if (acc_list.Count == 0)
            {
                Log.Warn("No pp query in PPShowConfig.json,defualt add 100%acc to get info.");
                acc_list.Add(100);
            }

            PP = new InfoOutputter(acc_list);

            PP.OnOutputEvent += OnOutput;
        }

        public void Exit()
        {
            var config_instance = new Config();
            foreach (var o in ListenOfs)
            {
                config_instance.listen_list.Add(new OutputConfig()
                {
                    output_file = o.outputter.FilePath,
                    output_format = o.formatter.FormatTemplate
                });
            }

            foreach (var o in PlayOfs)
            {
                config_instance.output_list.Add(new OutputConfig()
                {
                    output_file = o.outputter.FilePath,
                    output_format = o.formatter.FormatTemplate
                });
            }

            string json = JsonConvert.SerializeObject(config_instance, Formatting.Indented);
            File.WriteAllText(m_config_path, json);
        }

        private void OnOutput(OutputType output_type, Dictionary<string, string> data_dic)
        {
            CurrentOutputInfo = data_dic;

            CleanFileList(ListenOfs);
            CleanFileList(PlayOfs);

            switch (output_type)
            {
                case OutputType.Listen:
                    _OutputFiles(ListenOfs);
                    break;

                case OutputType.Play:
                    _OutputFiles(PlayOfs);
                    break;

                default:
                    break;
            }

            void _OutputFiles(List<OutputWrapper> list)
            {
                foreach (var output in list)
                {
                    string str = output.formatter.Format(data_dic);

                    if (PPShowAllowDumpInfo == true)
                    {
                        IO.CurrentIO.WriteColor("[PPShow][", ConsoleColor.White, false);
                        IO.CurrentIO.WriteColor($"{output.outputter.FilePath}", ConsoleColor.Red, false);

                        IO.CurrentIO.WriteColor("]", ConsoleColor.White, false);
                        IO.CurrentIO.WriteColor($"{str}", ConsoleColor.White);
                    }

                    try
                    {
                        output.outputter.Output(str);
                    }
                    catch (Exception e)
                    {
                        Log.Error(string.Format(PPSHOW_IO_ERROR, output.outputter.FilePath, e.Message));
                    }
                }
            }
        }

        private void CleanFileList(List<OutputWrapper> list)
        {
            foreach (var o in list)
            {
                try
                {
                    if (o.outputter is DiskFileOutput && !File.Exists(o.outputter.FilePath))
                    {
                        continue;
                    }

                    o.outputter.Output(o.formatter.Format(null));
                }
                catch (Exception e)
                {
                    Log.Error(string.Format(PPSHOW_IO_ERROR, o.outputter.FilePath, e.Message));
                }
            }
        }

        private void ListenClean()
        {
            CurrentOutputInfo = null;

            CleanFileList(PlayOfs);

            foreach (var o in ListenOfs)
            {
                try
                {
                    o.outputter.Output(o.formatter.Format(null));
                }
                catch (Exception e)
                {
                    Log.Error(string.Format(PPSHOW_IO_ERROR, o.outputter.FilePath, e.Message));
                }
            }
        }

        public bool Output(OutputType output_type, string osu_file_path, ModsInfo mods, params KeyValuePair<string, string>[] extra)
        {
            if (output_type == OutputType.Listen && String.IsNullOrWhiteSpace(osu_file_path))
            {
                ListenClean();
                return true;
            }

            return PP.TrigOutput(output_type, osu_file_path, mods, extra);
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
                    if (int.TryParse(result, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ival))
                        return ival;
                    else if (double.TryParse(result, NumberStyles.AllowThousands | NumberStyles.Float, CultureInfo.InvariantCulture, out var dval))
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
                        if (double.TryParse(pp, out var dval))
                            list.Add(new { acc, pp = dval });
                    }

                return list;
            }

            return null;
        }

        #endregion DDRP
    }
}