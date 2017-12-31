using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OsuLiveStatusPanel
{
    /// <summary>
    /// 格式${xxx[:acc]}
    /// xxx可为ar,cs,od,hp,pp,speed_stars,aim_stars,stars,mods,title,creator,max_combo,artist,circles,spinners
    /// acc在xxx为pp时为必须
    /// </summary>
    public class OutputFormatter
    {
        private string m_format;

        static Regex pattern = new Regex(@"\$\{(.+?)\}");

        public OutputFormatter(string format)
        {
            m_format = format;
        }

        public string Format(List<OppaiJson> oppais, Dictionary<string, string> data_dic)
        {
            string result_str=m_format;

            var result = pattern.Match(result_str);

            while (result.Success)
            {
                var key=result.Groups[1].Value.Trim();

                string val;

                if (!data_dic.TryGetValue(key, out val))
                {
                    val = String.Empty;
                }

                result_str = result_str.Replace(result.Value, val);

                result = result.NextMatch();
            }

            return result_str;
        }

        public List<float> GetAccuracyArray()
        {
            List<float> result = new List<float>();

            var m=Regex.Match(m_format, @"\$\{pp:\d{1,3}\.\d{2}%\}");

            while(m.Success)
            {
                var acc_m = Regex.Match(m.Groups[0].Value, @"\d{1,3}\.\d{2}");
                float acc = float.Parse(acc_m.Groups[0].Value);
                result.Add(acc);

                m = m.NextMatch();
            }

            return result;
        }
    }
}
