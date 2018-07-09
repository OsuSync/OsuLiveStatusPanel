using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace OsuLiveStatusPanel.PPShow
{
    /// <summary>
    /// 格式${xxx[:acc]}
    /// xxx可为ar,cs,od,hp,pp,speed_stars,aim_stars,stars,mods,title,creator,max_combo,artist,circles,spinners
    /// acc在xxx为pp时为必须
    /// </summary>
    public class OutputFormatter
    {
        public string FormatTemplate { get; set; }

        private static Regex pattern = new Regex(@"\$\{(.+?)\}");

        public OutputFormatter(string format)
        {
            FormatTemplate = format;
        }

        public string Format(Dictionary<string, string> data_dic)
        {
            string result_str = FormatTemplate;

            if (data_dic == null)
                return string.Empty;

            var result = pattern.Match(result_str);

            while (result.Success)
            {
                var key = result.Groups[1].Value.Trim();

                string val;

                if (!data_dic.TryGetValue(key, out val))
                {
                    val = String.Empty;
                }

                //简化一下
                if (int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out int idata))
                {
                    val = $"{idata}";
                }
                else if (float.TryParse(val, NumberStyles.AllowThousands | NumberStyles.Float, CultureInfo.InvariantCulture, out float fdata))
                {
                    val = $"{fdata:F2}";
                }

                result_str = result_str.Replace(result.Value, val);

                result = result.NextMatch();
            }

            return result_str;
        }

        public List<float> GetAccuracyArray()
        {
            List<float> result = new List<float>();

            var m = Regex.Match(FormatTemplate, @"\$\{pp:(.+?)%\}");

            while (m.Success)
            {
                var acc_m = m.Groups[1].Value.ToString();
                float acc = float.Parse(acc_m, CultureInfo.InvariantCulture);
                result.Add(acc);

                m = m.NextMatch();
            }

            return result;
        }
    }
}