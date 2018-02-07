using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OsuLiveStatusPanel
{
    public static class BeatmapReader
    {
        static Regex regex = new Regex(@"^(.+)\s*:\s*(.+)$");

        public static OppaiJson GetJsonFromFile(string filepath)
        {
            var json = new OppaiJson();

            using (StreamReader reader=new StreamReader(File.OpenRead(filepath)))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();

                    if (line.Trim()== "[Events]")
                        return json;

                    var match = regex.Match(line);

                    if (!match.Success)
                    {
                        continue;
                    }

                    string name = match.Groups[1].ToString();
                    string value = match.Groups[2].ToString();

                    switch (name)
                    {
                        case "Artist":
                            json.artist = value;
                            break;
                        case "Title":
                            json.title = value;
                            break;
                        case "Version":
                            json.version = value;
                            break;
                        case "Creator":
                            json.creator = value;
                            break;
                        case "OverallDifficulty":
                            json.od = float.Parse(value);
                            break;
                        case "ApproachRate":
                            json.ar = float.Parse(value);
                            break;
                        case "HPDrainRate":
                            json.hp = float.Parse(value);
                            break;
                        case "CircleSize":
                            json.cs = float.Parse(value);
                            break;
                        default:
                            break;
                    }
                }
            }

            return json;
        }
    }
}
