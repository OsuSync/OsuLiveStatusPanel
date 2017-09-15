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

        public OutputFormatter(string format)
        {
            m_format = format;
        }

        public string Format(List<OppaiJson> oppais)
        {
            string result=m_format;
            var type = oppais[0].GetType();
            var members=type.GetProperties();
            foreach (var m in members)
            {
                string member = m.Name;

                switch (member)
                {
                    case "mods_str":
                        member = "mods";
                        break;
                    case "num_circles":
                        member = "circles";
                        break;
                    case "num_spinners":
                        member = "spinners";
                        break;
                }

                string str = "";
                if(m.PropertyType==typeof(float))
                    str = $"{(float)m.GetValue(oppais[0]):F}";
                else
                    str = m.GetValue(oppais[0]).ToString();

                if (member == "pp")
                {
                    foreach (var oppai in oppais)
                    {
                        str = $"{(float)m.GetValue(oppai):F}";
                        result = result.Replace("${pp:" + $"{oppai.accuracy:F}" + "%}", str);
                    }

                }
                else
                {
                    result = result.Replace("${" + member + "}", str);
                }
            }

            return result;
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
