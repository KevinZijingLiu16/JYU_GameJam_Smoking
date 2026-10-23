# 三场景流程

打开 `Assets/Scenes/MainMenu.unity` 后运行。三个新场景已经加入 Build Settings，主菜单排第一。

## 场景

- **MainMenu**：Start Game 和 Restart 都从头加载 Gameplay（当前没有存档进度）；Settings 打开音量/亮度设置；Quit 在构建版本中退出应用，在 Unity 编辑器中结束 Play Mode。
- **Gameplay**：保留空的游戏世界，Canvas 提供 BGM 测试环境、Play Test SFX、Complete / Credits 两个测试按钮，以及使用 scaled time 的计时显示。ESC 打开暂停面板；可继续、打开设置、返回主菜单。设置页的 ESC 或 Back 先回到暂停页，再按 ESC 才继续游戏。
- **Credits**：空的游戏世界 + Canvas 滚动字幕 + Main Menu 返回按钮。以后在胜利逻辑中调用 `SceneFlow.CompleteGame()` 即可进入。

## 修改 UI

所有文字、按钮、滑块、面板都是真实保存在场景里的 uGUI / Canvas 对象，不由运行时代码生成。可直接修改 RectTransform、Image、TextMeshPro 文本和 Button 的 On Click。

- `Canvas/MainMenuPanel`：标题和四个按钮。
- `Canvas/SettingsPanel`：默认隐藏。编辑时手动启用即可查看，改完请关闭。
- Gameplay 的 `Canvas/PausePanel`：默认隐藏的暂停页。
- `Canvas/GameplayHUD`：测试提示和测试按钮，正式制作关卡时可自行删除/替换。
- 使用 Input System UI Input Module，支持鼠标及 UI 导航。编辑器里测试键盘时先点击 Game 窗口使其获得焦点。
- 当前 UI 使用英文和 TMP 默认字体。换成中文时需要给 TMP 指定包含中文字形的字体资产。

## 最低亮度

选中 `Assets/Script/GameFlow/Setup/GameSettings.asset`，调整 **Minimum Brightness**（默认 `0.35`）。

1 表示原始亮度，0.35 表示黑色遮罩不超过 65% 不透明度。程序会把保存值和滑块下限限制在该数值以上，避免全黑。Default Brightness 只用于没有已保存设置的首次启动。参数通常在编辑模式中配置，重新进入 Play Mode 后生效。

亮度通过各场景 `BrightnessCanvas/BrightnessShade` 实现，作用于整个画面（包括 UI），不改变显示器硬件亮度。遮罩不拦截鼠标操作。未来新增场景时复制亮度 Canvas 和两个启动物体即可复用设置。

## 音频

所有场景使用已接好的 `AudioBootstrap`，通过同一个 AudioMixer 控制 BGM / SFX。当前 BGM 为项目已有的 `track_stepbystep_loop.wav`，按钮音效为 `sfx_button1_click1.wav`。

在各场景 `SceneFlow` 的 **Background Music** 更换 AudioCue 即可换音乐；当前使用相同 cue，切场景不重新开始。暂停时 BGM 继续播放，音量/亮度实时生效；点击 Test Sound 可预览音效音量。

设置在关闭设置面板、切换场景、退出和应用暂停时保存到 PlayerPrefs。音频使用 `Smoking.Audio.*`，亮度使用 `Smoking.Display.Brightness`。

## 制作人和特别鸣谢

在 Credits 场景选中 `Canvas/CreditsViewport/CreditsText`，直接编辑 TextMeshPro 的文字内容。已留 Created By、Design & Development、Art & Audio、Special Thanks 占位段落。

在 `CreditsViewport` 上的 **Credits Scroller** 调整：

- Pixels Per Second：滚动速度。
- Start Delay：开始前等待时间。
- Loop：是否循环。

文本高度自动计算，增加名字后无需手动改高度。默认滚完停止，返回按钮始终可用。

## 代码职责与验证

GameSettingsService 保存共享设置；SettingsPanel 只连接滑块和服务；BrightnessOverlay 只显示亮度；SceneFlow 负责导航；MenuPanels 管理面板和暂停；CreditsScroller 只负责字幕滚动。

`Tests/GameFlowTests.cs` 覆盖跨场景设置、最低亮度、暂停/继续、暂停页返回、重新开始和字幕滚动。场景生成器 `Editor/GameSceneBuilder.cs` 仅用于首次创建缺失场景，不覆盖你已经编辑过的场景。
