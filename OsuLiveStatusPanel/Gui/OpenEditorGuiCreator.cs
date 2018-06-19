using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using ConfigGUI.ConfigurationRegion.ConfigurationItemCreators;
using OsuLiveStatusPanel.PPShow;
using Sync.Tools.ConfigurationAttribute;

namespace OsuLiveStatusPanel.Gui
{
    class OpenEditorGuiCreator:PathConfigurationItemCreator
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

            btn.Click += (s, e) =>
              {
                  new EditorWindow(m_wrapper).ShowDialog();
              };

            panel.Children.Add(btn);
            return panel;
        }
    }
}
