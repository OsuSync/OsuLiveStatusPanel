using ConfigGUI;
using OsuLiveStatusPanel.PPShow;
using Sync.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsuLiveStatusPanel.Gui
{
    static class GuiRegisterHelper
    {
        public static void RegisterConfigGui(Plugin plugin, InfoOutputterWrapper wrapper)
        {
            ConfigGuiPlugin gui = plugin as ConfigGuiPlugin;
            gui.ItemFactory.RegisterItemCreator<ConfigEditorAttribute>(new OpenEditorGuiCreator(wrapper));
        }
    }
}
