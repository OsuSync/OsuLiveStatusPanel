using ConfigGUI.ConfigurationRegion.ConfigurationItemCreators;
using OsuLiveStatusPanel.PPShow;
using Sync.Tools.ConfigurationAttribute;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace OsuLiveStatusPanel.Gui
{
    internal class OpenEditorGuiCreator : PathConfigurationItemCreator
    {
        private InfoOutputterWrapper m_wrapper;

        public OpenEditorGuiCreator(InfoOutputterWrapper wrapper)
        {
            m_wrapper = wrapper;
        }

        public override Panel CreateControl(BaseConfigurationAttribute attr, PropertyInfo prop, object configuration_instance)
        {
            Panel panel = base.CreateControl(attr, prop, configuration_instance);
            Button btn = new Button()
            {
                Content = "Open Editor"
            };

            EditorWindow window = null;

            btn.Click += (s, e) =>
              {
                  window = (window??new EditorWindow(m_wrapper));
                  if (window.Visibility == Visibility.Visible)
                      window.Activate();
                  else
                      window.Show();
              };

            panel.Children.Add(btn);
            return panel;
        }
    }
}