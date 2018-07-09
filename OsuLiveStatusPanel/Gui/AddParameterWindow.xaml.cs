using Sync;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using static OsuLiveStatusPanel.Gui.EditorWindow;

namespace OsuLiveStatusPanel.Gui
{
    /// <summary>
    /// Interaction logic for AddParameterWindow.xaml
    /// </summary>
    public partial class AddParameterWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private ConfigItemProxy m_currentProxy;

        public ConfigItemProxy CurrnetProxy
        {
            get => m_currentProxy;
            set
            {
                m_currentProxy = value;
                FormatEditBox.DataContext = m_currentProxy;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PPInputVisibility)));
            }
        }

        public bool IsModsDisplay => !(!m_modsChangeAtListen && m_currentProxy?.OutputType == OutputType.Listen);
        public Visibility PPInputVisibility => IsModsDisplay ? Visibility.Visible : Visibility.Collapsed;

        private static readonly List<string> s_olspParameter = new List<string>()
        {
            "ar", "cs", "od", "hp", "beatmap_setid", "version", "title_avaliable", "artist_avaliable",
            "beatmap_setlink", "beatmap_link", "beatmap_id", "min_bpm", "max_bpm", "speed_stars", "aim_stars",
            "stars", "mods", "title", "creator", "max_combo", "artist", "circles","sliders", "spinners"
        };

        private static readonly Dictionary<string, string> s_previewData = new Dictionary<string, string>()
        {
            ["ar"] = "10.00",
            ["cs"] = "5.20",
            ["od"] = "10.00",
            ["hp"] = "7.00",

            ["version"] = "Kaitei",
            ["creator"] = "Loreley",
            ["title"] = "Umiyuri Kaiteitan",
            ["artist"] = "GEM",
            ["title_avaliable"] = "ウミユリ海底譚",
            ["artist_avaliable"] = "ジェム",

            ["beatmap_setlink"] = "http://osu.ppy.sh/s/647452",
            ["beatmap_link"] = "http://osu.ppy.sh/b/1371599",
            ["beatmap_setid"] = "647452",
            ["beatmap_id"] = "1371599",

            ["min_bpm"] = "240",
            ["max_bpm"] = "240",

            ["speed_stars"] = "2.84",
            ["aim_stars"] = "4.02",
            ["stars"] = "7.44",

            ["max_combo"] = "1662",
            ["circles"] = "603",
            ["sliders"] = "522",
            ["spinners"] = "0"
        };

        private bool m_modsChangeAtListen = false;

        public AddParameterWindow()
        {
            InitializeComponent();
            dynamic ortdp = SyncHost.Instance.EnumPluings().First(p => p.Name == "OsuRTDataProvider");
            m_modsChangeAtListen = ortdp.ModsChangedAtListening;

            DataContext = this;

            FormatEditBox.TextChanged += (s, e) =>
              {
                  var data = s_previewData;
                  if (IsModsDisplay)
                  {
                      var acc_list = m_currentProxy.RawObject.formatter.GetAccuracyArray();
                      data = data.Union(acc_list.Select(acc => new KeyValuePair<string, string>($"pp:{acc:F2}%", "727.00"))).ToDictionary(p => p.Key, p => p.Value);
                      data.Add("mods", "HD,HR");
                  }
                  FormatPreviewBox.Text = m_currentProxy?.Format(data);
              };

            foreach (var para in s_olspParameter)
            {
                var btn = new Button()
                {
                    Content = para.Replace("_", "__"),
                    Margin = new Thickness(2)
                };

                if (para == "mods")
                {
                    btn.SetBinding(Button.VisibilityProperty, new Binding("PPInputVisibility") { Source = this });
                }

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
            if (float.TryParse(AccTextBox.Text, NumberStyles.AllowThousands | NumberStyles.Float, CultureInfo.InvariantCulture, out float acc))
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