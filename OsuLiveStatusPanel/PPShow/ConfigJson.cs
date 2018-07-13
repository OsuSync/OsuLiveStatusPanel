﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

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
            string config_json = File.ReadAllText(config_path);

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