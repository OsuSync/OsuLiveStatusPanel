using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace OsuLiveStatusPanel.PPShow
{
    public class OutputConfig
    {
        public string output_format;
        public string output_file;
    }

    public class Config
    {
        public List<OutputConfig> output_list = new List<OutputConfig>();
        public List<OutputConfig> listen_list = new List<OutputConfig>();

        public Config()
        {
        }

        public static Config LoadPPShowConfig(string config_path)
        {
            string config_json = CheckConfigContextFloatValueGlobalizable(File.ReadAllText(config_path));

            try
            {
                return JsonConvert.DeserializeObject<Config>(config_json);
            }
            catch (Exception e)
            {
                Log.Error($"JsonConvert::DeserializeObject Error,{e.Message}");
            }
            return null;
        }

        /// <summary>
        /// 将看上去不符合当前用户环境的浮点值转换成合适的格式
        /// 比如在俄罗斯地区将会把"${pp:12.23%}"转换成"${pp:12,23%}"
        /// </summary>
        /// <param name="config_content"></param>
        /// <returns></returns>
        private static string CheckConfigContextFloatValueGlobalizable(string config_content)
        {
            Log.Debug($"Current CultureInfo:{CultureInfo.CurrentCulture.Name} NumberDecimalSeparator:{CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator}");
            Log.Debug("Check if config content exsit unknown float format...\n");

            Regex regex = new Regex(@"\$\{pp:(\d+((.)\d+)?)%\}");
            var match = regex.Match(config_content);

            while (match.Success)
            {
                Log.Debug($"-Found PP query format:{match.Value} \t {(match.Groups[3].Value != CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator && !string.IsNullOrWhiteSpace(match.Groups[3].Value) ? $"unknown separator:{match.Groups[3].Value.ToString()}" : string.Empty)}");

                if (match.Groups[3].Value != CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator && !string.IsNullOrWhiteSpace(match.Groups[3].Value))
                {
                    var adjust_param = $"{match.Value}".Replace(match.Groups[3].Value.ToString(), CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);

                    config_content = config_content.Replace(match.Value, adjust_param);

                    var test_val = (int)float.Parse(match.Groups[1].Value.Replace(match.Groups[3].Value, CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator));

                    Log.Debug($"-Adjust float format:{match.Value} -> {adjust_param} val:{test_val}\n");
                }

                match = match.NextMatch();
            }

            return config_content;
        }

        public static void CreateDefaultPPShowConfig(string config_path)
        {
            //init
            Config default_config = new Config();

            #region Default Output List

            default_config.output_list.Add(new OutputConfig()
            {
                output_file = "..\\output\\PP.txt",
                output_format = $"92%:${{pp:{92.00.ToString("F2", CultureInfo.CurrentCulture)}%}}pp 94%:${{pp:{94.00.ToString("F2", CultureInfo.CurrentCulture)}%}}pp 96%:${{pp:{96.00.ToString("F2", CultureInfo.CurrentCulture)}%}}pp 98%:${{pp:{98.00.ToString("F2", CultureInfo.CurrentCulture)}%}}pp 100%:${{pp:{100.00.ToString("F2", CultureInfo.CurrentCulture)}%}}pp"
            });

            default_config.output_list.Add(new OutputConfig()
            {
                output_file = "..\\output\\map_info.txt",
                output_format = "CS:${cs} \nAR:${ar} \nOD:${od} \nHP:${hp} \n \nStars:${stars}* \nAim:${aim_stars}* \nSpeed:${speed_stars}* \n \nMaxCombo:${max_combo}"
            });

            default_config.output_list.Add(new OutputConfig()
            {
                output_file = "..\\output\\mods.txt",
                output_format = "Mods:${mods}"
            });

            default_config.output_list.Add(new OutputConfig()
            {
                output_file = "..\\output\\current_playing.txt",
                output_format = "CurrentPlaying:${artist_avaliable} - ${title_avaliable} [${version}]"
            });

            default_config.output_list.Add(new OutputConfig()
            {
                output_file = "..\\output\\current_playing_map_info.txt",
                output_format = "Creator:${creator} \t Link:${beatmap_link}"
            });

            #endregion Default Output List

            #region Default (No NowPlaying) Listen List

            default_config.listen_list.Add(new OutputConfig()
            {
                output_file = "..\\output\\PP.txt",
                output_format = $"92%:${{pp:{92.00.ToString("F2", CultureInfo.CurrentCulture)}%}}pp 94%:${{pp:{94.00.ToString("F2", CultureInfo.CurrentCulture)}%}}pp 96%:${{pp:{96.00.ToString("F2", CultureInfo.CurrentCulture)}%}}pp 98%:${{pp:{98.00.ToString("F2", CultureInfo.CurrentCulture)}%}}pp 100%:${{pp:{100.00.ToString("F2", CultureInfo.CurrentCulture)}%}}pp"
            });

            default_config.listen_list.Add(new OutputConfig()
            {
                output_file = "..\\output\\map_info.txt",
                output_format = "CS:${cs} \nAR:${ar} \nOD:${od} \nHP:${hp} \n \nStars:${stars}* \nAim:${aim_stars}* \nSpeed:${speed_stars}* \n \nMaxCombo:${max_combo}"
            });

            default_config.listen_list.Add(new OutputConfig()
            {
                output_file = "..\\output\\mods.txt",
                output_format = "Mods:${mods}"
            });

            default_config.listen_list.Add(new OutputConfig()
            {
                output_file = "..\\output\\current_playing.txt",
                output_format = "CurrentListening:${artist_avaliable} - ${title_avaliable} [${version}]"
            });

            default_config.listen_list.Add(new OutputConfig()
            {
                output_file = "..\\output\\current_playing_map_info.txt",
                output_format = "Creator:${creator} \t Link:${beatmap_link}"
            });

            #endregion Default (NowPlaying) Listen List

            //
            File.WriteAllText(config_path, JsonConvert.SerializeObject(default_config, Formatting.Indented));
        }
    }
}