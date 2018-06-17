using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static OsuLiveStatusPanel.Gui.EditorWindow;

namespace OsuLiveStatusPanel.Gui
{
    /// <summary>
    /// Interaction logic for AddParameterWindow.xaml
    /// </summary>
    public partial class AddParameterWindow : Window
    {
        private ConfigItemProxy m_currentProxy;
        public ConfigItemProxy CurrnetProxy
        {
            get => m_currentProxy;
            set
            {
                m_currentProxy = value;
                FormatEditBox.DataContext = m_currentProxy;
            }
        }

        private static readonly List<string> s_olspParameter = new List<string>()
        {
            "ar", "cs", "od", "hp", "beatmap_setid", "version", "title_avaliable", "artist_avaliable",
            "beatmap_setlink", "beatmap_link", "beatmap_id", "min_bpm", "max_bpm", "speed_stars", "aim_stars",
            "stars", "mods", "title", "creator", "max_combo", "artist", "circles","sliders", "spinners"
        };

        private static readonly Dictionary<string, string> s_previewData = new Dictionary<string, string>()
        {
            ["ar"]="10.00",
            ["cs"]="5.46",
            ["od"]="10.00",
            ["hp"]="5.60",
            ["beatmap_setid"]= "546384",
            ["version"]= "NiNo's Extra",
            ["title_avaliable"]= "Brain Power",
            ["artist_avaliable"]= "NOMA",
            ["beatmap_setlink"]= "http://osu.ppy.sh/s/546384",
            ["beatmap_link"] = "http://osu.ppy.sh/b/1183809",
            ["beatmap_id"] = "1183809",
            ["min_bpm"]="170",
            ["max_bpm"] = "173",
            ["speed_stars"]="2.89",
            ["aim_stars"]="3.04",
            ["stars"]="6.01",
            ["mods"]="HD,HR",
            ["title"]= "Brain Power",
            ["creator"]= "Monstrata",
            ["max_combo"]="936",
            ["artist"] = "NOMA",
            ["circles"] = "281",
            ["sliders"] = "319",
            ["spinners"] = "0"
        };

        public AddParameterWindow()
        {
            InitializeComponent();

            FormatEditBox.TextChanged += (s, e) =>
              {
                  var acc_list = m_currentProxy.RawObject.formatter.GetAccuracyArray();
                  var data = s_previewData.Union(acc_list.Select(acc => new KeyValuePair<string, string>($"pp:{acc:F2}%", "727.00"))).ToDictionary(p=>p.Key,p=>p.Value);
                  FormatPreviewBox.Text = m_currentProxy?.Format(data);
              };

            foreach (var para in s_olspParameter)
            {
                var btn = new Button()
                {
                    Content = para.Replace("_","__"),
                    Margin = new Thickness(2)
                };

                btn.Click += (s, e) =>
                  {
                      m_currentProxy.FormatTemplate += $"${{{para}}}";
                  };

                ButtonsList.Children.Add(btn);
            }
            
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private void AddPP_Click(object sender, RoutedEventArgs e)
        {
            if (float.TryParse(AccTextBox.Text, out float acc))
            {
                if (0 <= acc && acc <= 100.0)
                    m_currentProxy.FormatTemplate += "${pp:" + $"{acc:F2}" + "%}";
                else
                    MessageBox.Show($"Accuracy should be in the range of 0 to 100", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                MessageBox.Show($"{AccTextBox.Text} not a decimal", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
