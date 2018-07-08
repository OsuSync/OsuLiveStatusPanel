using OsuLiveStatusPanel.Mods;
using Sync.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace OsuLiveStatusPanel.PPShow.Beatmap
{
    public static class BeatmapParser
    {
        private static readonly Dictionary<string, string> METADATA_MAP = new Dictionary<string, string> {
            {"BeatmapID","beatmap_id"},
            {"Source","source"},
            {"BeatmapSetID","beatmap_setid"},
            {"TitleUnicode","title_unicode"},
            {"ArtistUnicode","artist_unicode"},
            {"Artist","artist"},
            {"Title","title"},
            {"Version","version"},
            {"Tags","tags"},
            {"Creator","creator"}
        };

        private static readonly Dictionary<string, string> DIFFICALUT_MAP = new Dictionary<string, string>
        {
            {"HPDrainRate","hp"},
            {"CircleSize","cs"},
            {"OverallDifficulty","od"},
            {"ApproachRate","ar"}
        };

        private static readonly Dictionary<string, string> GENERAL_MAP = new Dictionary<string, string>
        {
            {"Mode","mode"}
        };

        private const int TYPE_CIRCLE = 1;
        private const int TYPE_SLIDER = 2;
        private const int TYPE_SPINER = 8;

        private static bool IS_TYPE(int value, int type) => (type & value) != 0;

        public static void ParseBeatmap(string file_path, Dictionary<string, string> extra_data, ModsInfo mods, out byte[] beatmap_data, out uint data_length)
        {
            int status = 0;
            int nobjects = 0, ncircle = 0, nslider = 0, nspiner = 0;
            double min_bpm = int.MaxValue, max_bpm = int.MinValue, current_bpm = 0;

            beatmap_data = File.ReadAllBytes(file_path);
            data_length = (uint)beatmap_data.Length;

            using (StreamReader reader = new StreamReader(new MemoryStream(beatmap_data, false)))
            {
                //简单的状态机
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    switch (status)
                    {
                        case 0: //seeking
                            if (line == "[General]")
                            {
                                status = 5;
                            }

                            if (line == "[Metadata]")
                            {
                                status = 1;
                            }

                            if (line == "[Difficulty]")
                            {
                                //status = 4; //不需要
                            }

                            if (line == "[TimingPoints]")
                            {
                                status = 2;
                            }

                            if (line == "[HitObjects]")
                            {
                                status = 3;
                            }

                            break;

                        case 1: //Metadata
                            if (string.IsNullOrWhiteSpace(line))
                            {
                                status = 0;
                                break;
                            }

                            foreach (var pair in METADATA_MAP)
                                if (line.StartsWith(pair.Key))
                                {
                                    extra_data[pair.Value] = line.Remove(0, pair.Key.Length + 1).Trim();
                                    break;
                                }

                            break;

                        case 5: //General
                            if (string.IsNullOrWhiteSpace(line))
                            {
                                status = 0;
                                break;
                            }

                            foreach (var pair in GENERAL_MAP)
                                if (line.StartsWith(pair.Key))
                                {
                                    extra_data[pair.Value] = line.Remove(0, pair.Key.Length + 1).Trim();
                                    break;
                                }

                            break;

                        case 4: //Difficulty
                            if (string.IsNullOrWhiteSpace(line))
                            {
                                status = 0;
                                break;
                            }

                            foreach (var pair in DIFFICALUT_MAP)
                                if (line.StartsWith(pair.Key))
                                {
                                    extra_data[pair.Value] = line.Remove(0, pair.Key.Length + 1).Trim();
                                    break;
                                }

                            break;

                        case 2: //TimingPoints
                            if (string.IsNullOrWhiteSpace(line))
                            {
                                min_bpm = Math.Round(min_bpm, MidpointRounding.AwayFromZero);
                                max_bpm = Math.Round(max_bpm, MidpointRounding.AwayFromZero);

                                if (mods.HasMod(ModsInfo.Mods.DoubleTime) || mods.HasMod(ModsInfo.Mods.Nightcore))
                                {
                                    min_bpm *= 1.5;
                                    max_bpm *= 1.5;
                                    min_bpm = Math.Round(min_bpm, MidpointRounding.AwayFromZero);
                                    max_bpm = Math.Round(max_bpm, MidpointRounding.AwayFromZero);
                                }
                                if (mods.HasMod(ModsInfo.Mods.HalfTime))
                                {
                                    min_bpm *= 0.75;
                                    max_bpm *= 0.75;
                                    min_bpm = Math.Round(min_bpm, MidpointRounding.AwayFromZero);
                                    max_bpm = Math.Round(max_bpm, MidpointRounding.AwayFromZero);
                                }
#if DEBUG
                                IO.CurrentIO.Write($"[Oppai]BPM:{min_bpm} ~ {max_bpm}");
#endif

                                extra_data["min_bpm"] = min_bpm.ToString();
                                extra_data["max_bpm"] = max_bpm.ToString();

                                status = 0;
                                break;
                            }

                            bool is_red_line = true;
                            string[] data = line.Split(',');

                            if (data.Length >= 7)
                            {
                                if (data[6] == "1" || string.IsNullOrWhiteSpace(data[6]))
                                    is_red_line = true;
                                else
                                    is_red_line = false;
                            }

                            if (!is_red_line) break;//1是红线

                            double val = double.Parse(data[1],CultureInfo.InvariantCulture);

                            if (val > 0)
                            {
                                val = 60000 / val;
                                current_bpm = val;
                            }
                            else
                            {
                                double mul = Math.Abs(100 + val) / 100.0f;
                                val = current_bpm * (1 + mul);
                                break;
                            }

                            min_bpm = Math.Min(val, min_bpm);
                            max_bpm = Math.Max(val, max_bpm);
                            break;

                        case 3:
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                var obj_data = line.Split(',');

                                if (obj_data.Length >= 4)
                                {
                                    nobjects++;
                                    int type = int.Parse(obj_data[3], CultureInfo.InvariantCulture);

                                    if (IS_TYPE(type, TYPE_CIRCLE))
                                        ncircle++;
                                    else if (IS_TYPE(type, TYPE_SLIDER))
                                        nslider++;
                                    else if (IS_TYPE(type, TYPE_SPINER))
                                        nspiner++;
                                }
                            }
                            break;

                        default:
                            break;
                    }
                }
            }

            extra_data["num_objects"] = nobjects.ToString();
            extra_data["num_circles"] = ncircle.ToString();
            extra_data["num_sliders"] = nslider.ToString();
            extra_data["num_spinners"] = nspiner.ToString();
        }
    }
}