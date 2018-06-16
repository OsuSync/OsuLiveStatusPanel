using OsuLiveStatusPanel.PPShow;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
using static OsuLiveStatusPanel.PPShow.InfoOutputterWrapper;

namespace OsuLiveStatusPanel.Gui
{
    /// <summary>
    /// Interaction logic for EditorWindow.xaml
    /// </summary>
    public partial class EditorWindow : Window
    {
        public class ConfigItemProxy : INotifyPropertyChanged
        {
            public OutputType OutputType { get; private set; }
            private OutputWrapper m_wrap;

            public ConfigItemProxy(OutputWrapper wrap, OutputType type)
            {
                m_wrap = wrap;
                OutputType = type;
            }

            public string FormatTemplate
            {
                get => m_wrap.formatter.FormatTemplate;
                set
                {
                    m_wrap.formatter.FormatTemplate = value;
                    NotifyPropertyChanged(nameof(FormatTemplate));
                }
            }

            public string FilePath
            {
                get => m_wrap.outputter.FilePath;
                set
                {
                    m_wrap.outputter.FilePath = value;
                    NotifyPropertyChanged(nameof(FilePath));
                }
            }

            public string Format(Dictionary<string, string> dict) => m_wrap.formatter.Format(dict);

            #region Notify Property Changed
            public event PropertyChangedEventHandler PropertyChanged;

            private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }
            #endregion
        }

        class ConfigItem
        {
            public ConfigItemProxy Proxy { get;private set; }

            public ConfigItem(OutputWrapper wrap, OutputType type)
            {
                Proxy = new ConfigItemProxy(wrap,type);
            }

            public BrowseCommand Browse { get; } = new BrowseCommand();
            public AddOutputParameterCommand AddOutputParameter { get; } = new AddOutputParameterCommand();

            public class AddOutputParameterCommand : ICommand
            {
                public event EventHandler CanExecuteChanged;

                public bool CanExecute(object parameter) => true;

                private static AddParameterWindow m_listenWindow;
                private static AddParameterWindow m_playWindow;

                public void Execute(object parameter)
                {
                    if (m_listenWindow == null)
                        m_listenWindow = new AddParameterWindow();

                    if (m_playWindow == null)
                        m_playWindow = new AddParameterWindow();

                    var proxy = parameter as ConfigItemProxy;
                    AddParameterWindow window = proxy.OutputType == OutputType.Listen ? m_listenWindow : m_playWindow;

                    window.CurrnetProxy = proxy;
                    window.ShowDialog();
                }
            }

            public class BrowseCommand : ICommand
            {
                public event EventHandler CanExecuteChanged;

                public bool CanExecute(object parameter) => true;

                public void Execute(object parameter)
                {
                    var proxy = parameter as ConfigItemProxy;
                    var fileDialog = new System.Windows.Forms.OpenFileDialog();
                    fileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(proxy.FilePath);
                    fileDialog.RestoreDirectory = true;
                    if(fileDialog.ShowDialog()==System.Windows.Forms.DialogResult.OK)
                        proxy.FilePath=fileDialog.FileName;
                }
            }
        }

        private InfoOutputterWrapper m_wrapper;
        private ObservableCollection<ConfigItem> listen_list;
        private ObservableCollection<ConfigItem> play_list;

        public EditorWindow(InfoOutputterWrapper wrapper)
        {
            InitializeComponent();

            m_wrapper = wrapper;

            listen_list = new ObservableCollection<ConfigItem>(wrapper.ListenOfs.Select(w => new ConfigItem(w,OutputType.Listen)));
            play_list = new ObservableCollection<ConfigItem>(wrapper.PlayOfs.Select(w => new ConfigItem(w, OutputType.Play)));

            ListenList.ItemsSource = listen_list;
            PlayList.ItemsSource = play_list;
        }
    }
}
