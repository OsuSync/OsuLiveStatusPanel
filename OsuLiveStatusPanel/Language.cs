using Sync.Tools;
using Sync.Tools.ConfigurationAttribute;

namespace OsuLiveStatusPanel
{
    public class Languages : I18nProvider
    {
        public static LanguageElement COMMAND_DESC = "获取屙屎状态面板的数据";
        public static LanguageElement COMMAND_HELP = "本插件可以通过NowPlaying/OsuRTDataProvider插件的来源提供当前游戏铺面.可在命令livestatuspanel后面添加参数来执行各种操作:\n\thelp\t:显示本插件帮助\n\tstatus\t:显示本插件当前状态\n\trestart\t:重新初始化本插件并重新读取配置";
        public static LanguageElement CONNAND_STATUS = "当前依赖的插件:{0}\n当前读取的PPShow配置文件:{1}";
        public static LanguageElement LOAD_PLUGIN_DEPENDENCY_FAILED = "加载本插件依赖的插件失败,原因";
        public static LanguageElement INIT_PLUGIN_FAILED_CAUSE_NO_DEPENDENCY = "初始化本插件失败,请检查Sync是否有NowPlaying插件或者Osu!RTDataProvider插件,以及是否config.ini中对应配置正确.";
        public static LanguageElement INIT_SUCCESS = "初始化成功~";
        public static LanguageElement REINIT_SUCCESS = "重新初始化完成~";
        public static LanguageElement OSURTDP_FOUND = "找到插件Osu!RTDP.";
        public static LanguageElement OSURTDP_NOTFOUND = "Osu!RTDP未找到,请检查Sync中的插件目录是否存在这货.";
        public static LanguageElement NOWPLAYING_FOUND = "找到插件NowPlaying";
        public static LanguageElement NOWPLAYING_NOTFOUND = "NowPlaying未找到,请检查Sync中的插件目录是否存在这货.";
        public static LanguageElement OSU_PROCESS_NOTFOUND = "未找到当前运行的屙屎程序";
        public static LanguageElement CLEAN_STATUS = "清理信息";
        public static LanguageElement NO_BEATMAP_PATH = "无法获取当前铺面的osu文件路径";
        public static LanguageElement IMAGE_NOT_FOUND = "图片路径不存在:";
        public static LanguageElement CANT_PROCESS_IMAGE = "无法处理或者保存图片";

        public static LanguageElement PPSHOW_BEATMAP_PARSE_ERROR = "Beatmap无法打开或解析,错误:";
        public static LanguageElement PPSHOW_FINISH = "执行结束,用时";
        public static LanguageElement PPSHOW_CONFIG_NOT_FOUND = "不存在指定路径的PPShowPlugin的配置文件{0}.现在已经创建默认配置文件，请自行配置";
        public static LanguageElement PPSHOW_CONFIG_PARSE_ERROR = "无法解析指定的PPShow配置文件";
        public static LanguageElement PPSHOW_IO_ERROR = "[PPShow]无法写入{0},原因{1}";

        public static GuiLanguageElement BeatmapSourcePlugin = "使用哪种插件作为铺面来源(ortdp/np二选一)";
        public static GuiLanguageElement Width = "固定图片宽度(EnableScaleClipOutputImageFile=1有效)";
        public static GuiLanguageElement Height = "固定图片后高度(EnableScaleClipOutputImageFile=1有效)";
        public static GuiLanguageElement EnableOutputModPicture = "是否生成Mod图片并输出";
        public static GuiLanguageElement OutputModImageFilePath = "生成的Mod图片保存路径";
        public static GuiLanguageElement ModUnitPixel = "每个Mod图片的大小(屙屎皮肤一般都是90*90)";
        public static GuiLanguageElement ModUnitOffset = "反转Mod传入顺序";
        public static GuiLanguageElement ModSortReverse = "是否逆序传入Mod";
        public static GuiLanguageElement ModDrawReverse = "是否要从右到左(从下到上)依次绘制mod图片(否则相反)";
        public static GuiLanguageElement ModUse2x = "是否钦定使用@2x结尾的源Mod图片";
        public static GuiLanguageElement ModSkinPath = "优先选择的Mod皮肤文件夹路径(如果这个文件夹没mod图片,再去当前打图皮肤文件夹找,默认皮肤玩家请使用这个强制选择要输出的图片)";
        public static GuiLanguageElement ModIsHorizon = "是否水平排列输出(否则垂直)";
        public static GuiLanguageElement EnableScaleClipOutputImageFile = "是否按固定分辨率输出背景图片(否则会直接复制图片到钦定输出路径)";
        public static GuiLanguageElement EnableListenOutputImageFile = "选图界面是否也会输出背景图片(钦定ORTDP源)";
        public static GuiLanguageElement PPShowJsonConfigFilePath = "PPShowPlugin配置文件路径";
        public static GuiLanguageElement PPShowAllowDumpInfo = "是否允许内置的PPShowPlugin输出解析结果在Sync程序内";
        public static GuiLanguageElement OutputBackgroundImageFilePath = "输出背景图片文件路径";
        public static GuiLanguageElement EnableOutputBackgroundImage = "是否输出背景图片";
    }
}