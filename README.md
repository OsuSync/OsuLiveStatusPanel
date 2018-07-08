# OsuLiveStatusPanel
---
## 注意
此为另一个软件OsuSync的插件,主要提供于osu当前游戏中的谱面信息(比如beatmapID/SetID，PP等),并将内容写入各文件再供obs之类的软件使用。

**此插件还没准备好正式公开，还在测试当中**
**The plug-in is not ready to be officially released, it is still being tested.**

## 使用到的其他项目
* [OsuSync (MIT License)](https://github.com/Deliay/Sync)
* [OsuSync/NowPlayingPlugin (MIT License)](https://github.com/Deliay/SyncPlugin/tree/master/NowPlaying)
* [PPShowPlugin](https://coding.net/u/KedamaOvO/p/PPShowPlugin/git)(已经内置,不再支持外置调用)
* [oppai (GPL-3.0 License)](https://github.com/Francesco149/oppai)
* [OsuRTDataProvider(原MemoryReader)](https://github.com/KedamaOvO/OsuRTDataProvider-Release)

## 截图
![](https://puu.sh/zgbjf/75e7809432.jpg)
![](https://puu.sh/xAeUS/3fd87076b7.png)
![](https://puu.sh/xAeKe/e3bb87eba6.png)

## 使用方法以及自定义输出
请去[Wiki](https://github.com/MikiraSora/OsuLiveStatusPanel/wiki)

## 关于Config.ini中各个设置的解释
| 设置名称     | 值|默认值| 描述|
|:---------|:---------|:---------|:-------|
| OutputBackgroundImageFilePath | string |..\output_result.png| 输出模糊图片文件路径     |
| AllowUsedMemoryReader | 0/1 |0| 是否允许使用MemoryReader插件来获取当前谱面信息(和AllowUsedNowPlaying二选一)     |
| AllowUsedNowPlaying | 0/1 |1| 是否允许使用NowPlaying插件来获取当前谱面信息(和AllowUsedMemoryReader二选一)     |
| PPShowJsonConfigFilePath | string |..\PPShowConfig.json| PPShowPlugin配置文件路径     |
|PPShowAllowDumpInfo|0/1|1|是否允许内置的PPShowPlugin输出解析结果在Sync程序内|
|DebugOutputBGMatchFailedListFilePath|string|..\failed_list.txt|匹配背景图失败的osu路径|
|EnableOutputModPicture|0/1|0|是否生成Mod图片并输出|
|OutputModImageFilePath|string|..\output_mod.png|生成的Mod图片保存路径|
|ModUnitPixel|uint|90|每个Mod图片的大小(屙屎皮肤一般都是90*90)|
|ModSortReverse|0/1|1|反转Mod传入顺序
|ModDrawReverse|0/1|1|是否要从右到左(从下到上)依次绘制mod图片(否则相反)
|ModUnitOffset|uint|10|每个Mod图片相距|
|ModUse2x|0/1|0|是否钦定使用@2x结尾的源Mod图片|
|ModSkinPath|string||优先选择的Mod皮肤文件夹路径(如果这个文件夹没mod图片,再去当前打图皮肤文件夹找,打图默认皮肤玩家请使用这个强制选择要输出的图片)|
|ModIsHorizon|0/1|1|是否水平排列输出(否则垂直)|
|EnableScaleClipOutputImageFile|0/1|1|是否按固定分辨率输出背景图片(否则会直接复制图片到钦定输出路径)|
| Width | uint |1920| 固定图片宽度(EnableScaleClipOutputImageFile=1有效)     |
| Height | uint |1080| 固定图片后高度(EnableScaleClipOutputImageFile=1有效)     |
|EnableListenOutputImageFile|0/1|1|选图界面是否也会输出背景图片(钦定ORTDP源)|

## 末尾
若有疑问、建议或出现bugs，可以创建issue或者私发邮件到mikirasora0409@126.com,欢迎提交PR
