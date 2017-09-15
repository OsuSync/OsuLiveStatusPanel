# OsuLiveStatusPanel
---

## 注意
此为另一个软件OsuSync的插件,主要提供于osu当前游戏中的谱面信息(比如beatmapID/SetID，PP等),并将内容写入各文件再供obs之类的软件使用。

## 使用到的其他项目
* [OsuSync (MIT License)](https://github.com/Deliay/Sync)
* [OsuSync/NowPlayingPlugin (MIT License)](https://github.com/Deliay/SyncPlugin/tree/master/NowPlaying)
* [PPShowPlugin](https://coding.net/u/KedamaOvO/p/PPShowPlugin/git)
* [oppai (GPL-3.0 License)](https://github.com/Francesco149/oppai)
* [MemoryReader]()

## 使用方法
从[这里](https://github.com/MikiraSora/OsuLiveStatusPanel/releases)下载我编译整理好的压缩包，将里面的内容直接解压到OsuSync根目录即可,然后可以自行修改参数.默认情况下，会在OsuSync根目录有一个output文件夹，里面就是输出各种内容

## 截图
![](https://puu.sh/xAeUS/3fd87076b7.png)
![](https://puu.sh/x95HP/94247ebd27.png)
![](https://puu.sh/xAeKe/e3bb87eba6.png)
若有疑问、建议或出现bugs，可以创建issue或者私发邮件到mikirasora0409@126.com,欢迎提交PR

## 关于Config.ini中各个设置的解释
| 设置名称     | 值|默认值| 描述|
|:---------|:---------|:---------|:-------|
| Width | uint |1920| 模糊图片后宽度      |
| Height | uint |1080| 模糊图片后高度     |
| LiveHeight | uint |1600| 模糊图片后活动高度     |
| LiveHeight | uint |900| 模糊图片后活动高度     |
| BlurRadius | uint |7| 高斯模糊半径     |
| FontSize | uint |15| 文本绘制字体半径     |
| EnablePrintArtistTitle | 0/1 |0| 是否允许直接将艺术家和标题直接写在模糊图片上并输出     |
| EnableAutoStartOutlayPPShowPlugin | 0/1 |1| 是否自动开启外置的PPShowPlugin.exe     |
| EnableUseBuildInPPShowPlugin | string |PPShowPlugin.exe| 外置的PPShowPlugin.exe文件路径     |
| OutputArtistTitleDiffFilePath | string |output_current_playing.txt| 输出铺面基本信息文件路径   |
| OutputOsuFilePath | string |in_current_playing.txt| 输出.osu文件路径     |
| OutputBeatmapNameInfoFilePath | string |output_current_playing_beatmap_info.txt| 输出谱面作者和Link文件路径     |
| OutputBackgroundImageFilePath | string |output_result.png| 输出模糊图片文件路径     |
| OutputBestLocalRecordInfoFilePath | string |output_best_local_record_info.txt| [暂时废置] 输出当前谱面最佳本地记录数据     |
| AllowUsedMemoryReader | 0/1 |0| 是否允许使用MemoryReader插件来获取当前谱面信息     |
| AllowUsedNowPlaying | 0/1 |1| 是否允许使用NowPlaying插件来获取当前谱面信息     |
| AllowGetDiffNameFromOsuAPI | 0/1 |1| 是否允许使用OsuAPI来获取谱面难度名称     |
| EnableGenerateBlurImageFile | 0/1 |1| 是否允许模糊谱面背景图片并输出     |
| EnableUseBuildInPPShowPlugin | 0/1 |1| 是否允许使用内置的PPShowPlugin     |
| EnableGenerateBlurImageFile | 0/1 |1| 是否允许模糊谱面背景图片并输出     |
| PPShowJsonConfigFilePath | string |..\PPShowConfig.json| PPShowPlugin配置文件路径     |
|PPShowAllowDumpInfo|0/1|1|是否允许内置的PPShowPlugin输出解析结果在Sync程序内
