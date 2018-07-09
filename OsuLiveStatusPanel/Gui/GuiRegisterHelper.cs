using ConfigGUI;
using OsuLiveStatusPanel.PPShow;
using Sync.Plugins;

namespace OsuLiveStatusPanel.Gui
{
    internal static class GuiRegisterHelper
    {
        public static void RegisterConfigGui(Plugin plugin, InfoOutputterWrapper wrapper)
        {
            Log.Output("Found ConfigGUI plugin, loaded and register OLSP custom config windows");
            ConfigGuiPlugin gui = plugin as ConfigGuiPlugin;
            gui.ItemFactory.RegisterItemCreator<ConfigEditorAttribute>(new OpenEditorGuiCreator(wrapper));
        }
    }
}