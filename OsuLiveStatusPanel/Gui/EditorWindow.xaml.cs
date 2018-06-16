using OsuLiveStatusPanel.PPShow;
using System;
using System.Collections.Generic;
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
        class ConfigProxy : INotifyPropertyChanged
        {
            private OutputWrapper m_wrap;

            public ConfigProxy(OutputWrapper wrap)
            {
                m_wrap = wrap;
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
            public ConfigProxy Proxy { get;private set; }

            public ConfigItem(OutputWrapper wrap)
            {
                Proxy = new ConfigProxy(wrap);
            }

            public BrowseCommand Browse { get; } = new BrowseCommand();
            public AddOutputParameterCommand AddOutputParameter { get; } = new AddOutputParameterCommand();

            public class AddOutputParameterCommand : ICommand
            {
                public event EventHandler CanExecuteChanged;

                public bool CanExecute(object parameter) => true;

                public void Execute(object parameter)
                {
                    var proxy = parameter as ConfigProxy;
                    MessageBox.Show(proxy.FormatTemplate);
                }
            }

            public class BrowseCommand : ICommand
            {
                public event EventHandler CanExecuteChanged;

                public bool CanExecute(object parameter) => true;

                public void Execute(object parameter)
                {
                    var proxy = parameter as ConfigProxy;
                    var fileDialog = new System.Windows.Forms.OpenFileDialog();
                    fileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(proxy.FilePath);
                    fileDialog.RestoreDirectory = true;
                    if(fileDialog.ShowDialog()==System.Windows.Forms.DialogResult.OK)
                        proxy.FilePath=fileDialog.FileName;
                }
            }
        }

        private InfoOutputterWrapper m_wrapper;
        private List<ConfigItem> listen_list;
        private List<ConfigItem> playing_list;

        public EditorWindow(InfoOutputterWrapper wrapper)
        {
            InitializeComponent();

            m_wrapper = wrapper;

            listen_list = wrapper.ListenOfs.Select(w => new ConfigItem(w)).ToList();
            playing_list = wrapper.PlayingOfs.Select(w => new ConfigItem(w)).ToList();

            ListenList.ItemsSource = listen_list;
            PlayList.ItemsSource = playing_list;
        }
    }
}
