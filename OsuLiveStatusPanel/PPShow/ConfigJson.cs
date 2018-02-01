using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace OsuLiveStatusPanel
{
    public class OutputConfig
    {
        public string output_format;
        public string output_file;
    }

    public class Config
    {
        public static Config Instance;

        public string oppai = "oppai.exe";
        public string input_file = "";
        public string input_format = "${beatmap_file}@${mods}";
        public List<OutputConfig> output_list = new List<OutputConfig>();
        public List<OutputConfig> listen_list = new List<OutputConfig>();

        private Config() { }
        
        public static void InitConfig(string config_path)
        {
            string config_json = File.ReadAllText(config_path);

            try
            {
                Instance = JsonConvert.DeserializeObject<Config>(config_json);
            }
            catch (Exception e)
            {
                Sync.Tools.IO.CurrentIO.WriteColor($"JsonConvert::DeserializeObject Error,{e.Message}",ConsoleColor.Red);
            }
        }

        public static void InitConfigFile(string config_path) => File.WriteAllText(config_path, JsonConvert.SerializeObject(new Config(), Formatting.Indented));
    }
}
