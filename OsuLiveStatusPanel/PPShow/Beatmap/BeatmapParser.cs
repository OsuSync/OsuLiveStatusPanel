using OsuLiveStatusPanel.Mods;
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
            {"Mode","mode"},
            {"AudioFilename","audio_file_name"}
        };

        private const int TYPE_CIRCLE = 1;
        private const int TYPE_SLIDER = 2;
        private const int TYPE_SPINER = 8;
        private const int TYPE_MANIA_HOLD = 128;

        private static bool IS_TYPE(int value, int type) => (type & value) != 0;

        public static void ParseBeatmap(ref byte[] beatmap_raw_data, Dictionary<string, string> extra_data, ModsInfo mods)
        {
            int status = 0;
            double min_playable_start = double.MaxValue, max_playable_end = double.MinValue;
            int nobjects = 0, ncircle = 0, nslider = 0, nspiner = 0;
            double min_bpm = int.MaxValue, max_bpm = int.MinValue, current_bpm = 0;
            
            using (StreamReader reader = new StreamReader(new MemoryStream(beatmap_raw_data)))
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
                                status = 4;
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
                                Log.Debug($"[Oppai]BPM:{min_bpm} ~ {max_bpm}");
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

                            double val = double.Parse(data[1], CultureInfo.InvariantCulture);

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

                                if (obj_data.Length >= 6)
                                {
                                    double? time = null;

                                    nobjects++;
                                    int type = int.Parse(obj_data[3]);

                                    if (IS_TYPE(type, TYPE_CIRCLE))
                                        ncircle++;
                                    else if (IS_TYPE(type, TYPE_SLIDER))
                                        nslider++;
                                    else if (IS_TYPE(type, TYPE_SPINER))
                                    {
                                        nspiner++;
                                        time = double.Parse(obj_data[5]);
                                    }
                                    else if (IS_TYPE(type, TYPE_MANIA_HOLD))
                                    {
                                        time = double.Parse(obj_data[4]);
                                    }

                                    var t = time??double.Parse(obj_data[2]);

                                    min_playable_start = Math.Min(t, min_playable_start);
                                    max_playable_end = Math.Max(t, max_playable_end);
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

            var playable_duration = (int)(max_playable_end - min_playable_start);
            extra_data["playable_duration"] = playable_duration.ToString();

            extra_data["playable_duration_min_part"] = (playable_duration / 1000 / 60).ToString();
            extra_data["playable_duration_sec_part"] = ((playable_duration / 1000) % 60).ToString();
        }
    }
}