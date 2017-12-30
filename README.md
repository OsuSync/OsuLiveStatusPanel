# OsuLiveStatusPanel
---

## 注意
此为另一个软件OsuSync的插件,主要提供于osu当前游戏中的谱面信息(比如beatmapID/SetID，PP等),并将内容写入各文件再供obs之类的软件使用。

## 使用到的其他项目
* [OsuSync (MIT License)](https://github.com/Deliay/Sync)
* [OsuSync/NowPlayingPlugin (MIT License)](https://github.com/Deliay/SyncPlugin/tree/master/NowPlaying)
* [PPShowPlugin](https://coding.net/u/KedamaOvO/p/PPShowPlugin/git)(已经内置,不再支持外置调用)
* [oppai (GPL-3.0 License)](https://github.com/Francesco149/oppai)
* [OsuRTDataProvider(原MemoryReader)](https://github.com/KedamaOvO/OsuRTDataProvider-Release)

## 截图
![](https://puu.sh/xAeUS/3fd87076b7.png)
![](https://puu.sh/x95HP/94247ebd27.png)
![](https://puu.sh/xAeKe/e3bb87eba6.png)

## 使用方法
从[这里](https://github.com/MikiraSora/OsuLiveStatusPanel/releases)下载我编译整理好的压缩包，**将里面的内容直接解压到OsuSync根目录即可**,然后可以自行修改参数.**默认情况下，会在OsuSync根目录有一个output文件夹，里面就是输出各种内容**

### 如何检查这货是否运作正常或者出现其他问题
* 在默认配置下:
0. 请先确认您的Sync是否已经实装NowPlaying插件或者MemoryReader插件.检查config.ini文件中的配置,(确认Nowplaying插件是否已经开启.)
![](https://puu.sh/yuyz5/1be983707c.png)
<br>**如果没出现那些插件配置文件内容,请先打开一次OsuSync程序并退出**.
1. 打开osu程序和osuSync,确定是否出现红框之类的内容
![](https://puu.sh/y9J3S/00ce29c620.png)
2. 进选图界面(**如果之前选择MemoryReader为源那就要选图一次再return**,MemoryReader才开始运作)
3. 随便选一张图打然而暂停( 否则直接return会触发清理内容操作的 :P )
4. 切出osu程序看看文件根目录是否多出png文件以及output文件夹是否多出文件,检查那些文件是否和你所打的图一样的信息
![](https://puu.sh/y9Jdf/ef62f18023.png)

* **此插件基于NowPlaying,使用前请务必配置好NowPlaying插件的设置**,MemoryReader是可选的插件,你可以选用MemoryReader插件来获取当前铺面信息(但你还是要NowPlaying插件),和Nowplaying有所不同的是,**MemoryReader支持获取当前铺面选用Mod**,如果你选用MemoryReader插件,那你可以在./output/PP.txt获取当前mod(默认配置).但因为后者MemoryReader的特殊性,**我们也不会为此MemoryReader插件的使用造成的损失负任何责任**,怂的话仅仅使用NowPlaying就可以,这是非常安全的.

* 因为osu历史原因,NowPlaying会捕捉不到极少部分图的消息,导致于本插件没能输出任何内容,这锅不背;也因为osu历史原因,少部分谱面捕捉不到,这锅本插件背了;不过通过开启EnableDebug=1和配置DebugOutputBGMatchFailedListFilePath来输出那些没匹配背景图成功的osu路径,并提交给我,由我来改进.

## 关于Config.ini中各个设置的解释
| 设置名称     | 值|默认值| 描述|
|:---------|:---------|:---------|:-------|
| Width | uint |1920| 模糊图片后宽度      |
| Height | uint |1080| 模糊图片后高度     |
| BlurRadius | uint |7| 高斯模糊半径     |
| OutputArtistTitleDiffFilePath | string |..\output_current_playing.txt| 输出铺面基本信息文件路径   |
| OutputBackgroundImageFilePath | string |..\output_result.png| 输出模糊图片文件路径     |
| AllowUsedMemoryReader | 0/1 |0| 是否允许使用MemoryReader插件来获取当前谱面信息(和AllowUsedNowPlaying二选一)     |
| AllowUsedNowPlaying | 0/1 |1| 是否允许使用NowPlaying插件来获取当前谱面信息(和AllowUsedMemoryReader二选一)     |
| AllowGetDiffNameFromOsuAPI | 0/1 |1| 是否允许使用OsuAPI来获取谱面难度名称     |
| EnableGenerateBlurImageFile | 0/1 |1| 是否允许模糊谱面背景图片并输出     |
| PPShowJsonConfigFilePath | string |..\PPShowConfig.json| PPShowPlugin配置文件路径     |
|PPShowAllowDumpInfo|0/1|1|是否允许内置的PPShowPlugin输出解析结果在Sync程序内|
|EnableDebug|0/1|0|是否允许捕捉不到背景图片的osu路径输出|
|DebugOutputBGMatchFailedListFilePath|string|..\failed_list.txt|匹配背景图失败的osu路径|

## 末尾
若有疑问、建议或出现bugs，可以创建issue或者私发邮件到mikirasora0409@126.com,欢迎提交PR
