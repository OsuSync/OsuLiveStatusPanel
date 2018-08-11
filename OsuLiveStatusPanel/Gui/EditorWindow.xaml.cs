using OsuLiveStatusPanel.PPShow;
using OsuLiveStatusPanel.PPShow.Output;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
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
            public OutputWrapper RawObject => m_wrap;
            public bool IsMMF => m_wrap.outputter is MemoryMappedFileOutput;

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

            #endregion Notify Property Changed
        }

        private class ConfigItem
        {
            public ConfigItemProxy Proxy { get; private set; }

            public ConfigItem(OutputWrapper wrap, OutputType type, EditorWindow window)
            {
                Proxy = new ConfigItemProxy(wrap, type);
                Delete = new DeleteCommand(window, this);
            }

            public bool IsFileBoxReadOnly => !Proxy.IsMMF;
            public Visibility DisplayBrowseButton => Proxy.IsMMF ? Visibility.Hidden : Visibility.Visible;

            public BrowseCommand Browse { get; } = new BrowseCommand();
            public AddOutputParameterCommand AddOutputParameter { get; } = new AddOutputParameterCommand();
            public DeleteCommand Delete { get; private set; }

            public class DeleteCommand : ICommand
            {
                private EditorWindow m_window;
                private ConfigItem m_item;

                public event EventHandler CanExecuteChanged
                {
                    add { }
                    remove { }
                }

                public DeleteCommand(EditorWindow window, ConfigItem item)
                {
                    m_window = window;
                    m_item = item;
                }

                public bool CanExecute(object parameter) => true;

                public void Execute(object parameter)
                {
                    var proxy = parameter as ConfigItemProxy;

                    var list = proxy.OutputType == OutputType.Listen ? m_window.listen_list : m_window.play_list;
                    var wrapper = proxy.OutputType == OutputType.Listen ? m_window.m_wrapper.ListenOfs : m_window.m_wrapper.PlayOfs;

                    list.Remove(m_item);
                    wrapper.Remove(m_item.Proxy.RawObject);
                }
            }

            public class AddOutputParameterCommand : ICommand
            {
                public event EventHandler CanExecuteChanged
                {
                    add { }
                    remove { }
                }

                public bool CanExecute(object parameter) => true;

                private static AddParameterWindow m_parameterWindow;

                public void Execute(object parameter)
                {
                    if (m_parameterWindow == null)
                        m_parameterWindow = new AddParameterWindow();

                    var proxy = parameter as ConfigItemProxy;

                    m_parameterWindow.CurrnetProxy = proxy;
                    m_parameterWindow.ShowDialog();
                }
            }

            public class BrowseCommand : ICommand
            {
                public event EventHandler CanExecuteChanged
                {
                    add { }
                    remove { }
                }

                public bool CanExecute(object parameter) => true;

                public void Execute(object parameter)
                {
                    var proxy = parameter as ConfigItemProxy;
                    var fileDialog = new System.Windows.Forms.OpenFileDialog();
                    fileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(proxy.FilePath);
                    fileDialog.RestoreDirectory = true;
                    if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        proxy.FilePath = fileDialog.FileName;
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

            listen_list = new ObservableCollection<ConfigItem>(wrapper.ListenOfs.Select(w => new ConfigItem(w, OutputType.Listen, this)));
            play_list = new ObservableCollection<ConfigItem>(wrapper.PlayOfs.Select(w => new ConfigItem(w, OutputType.Play, this)));

            ListenList.ItemsSource = listen_list;
            PlayList.ItemsSource = play_list;
        }

        private void AddFileOutputButton_Listen_Click(object sender, RoutedEventArgs e)
        {
            var item = new OutputWrapper()
            {
                formatter = new OutputFormatter(""),
                outputter = OutputBase.Create(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output.txt"))
            };
            m_wrapper.ListenOfs.Add(item);
            listen_list.Add(new ConfigItem(item, OutputType.Listen, this));
        }

        private void AddMMFOutputButton_Listen_Click(object sender, RoutedEventArgs e)
        {
            var item = new OutputWrapper()
            {
                formatter = new OutputFormatter(""),
                outputter = OutputBase.Create("mmf://olsp-new")
            };
            m_wrapper.ListenOfs.Add(item);
            listen_list.Add(new ConfigItem(item, OutputType.Listen, this));
        }

        private void AddFileOutputButton_Play_Click(object sender, RoutedEventArgs e)
        {
            var item = new OutputWrapper()
            {
                formatter = new OutputFormatter(""),
                outputter = OutputBase.Create(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output.txt"))
            };
            m_wrapper.PlayOfs.Add(item);
            play_list.Add(new ConfigItem(item, OutputType.Listen, this));
        }

        private void AddMMFOutputButton_Play_Click(object sender, RoutedEventArgs e)
        {
            var item = new OutputWrapper()
            {
                formatter = new OutputFormatter(""),
                outputter = OutputBase.Create("mmf://olsp-new")
            };
            m_wrapper.PlayOfs.Add(item);
            play_list.Add(new ConfigItem(item, OutputType.Listen, this));
        }

        private void EditorWindow_OnClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }
    }
}