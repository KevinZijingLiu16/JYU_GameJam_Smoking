# Audio system

命名空间：`Smoking.Audio`。仅使用 Unity 内置 AudioSource / AudioMixer，无额外依赖。

## 接入

1. 将 `Setup/AudioBootstrap.prefab` 拖入场景，已连接 `Setup/GameAudio.mixer`，无需手动配置混音器。每个独立启动的场景都可以放一份；第一份跨场景保留，后续实例只负责转发场景引用。
2. AudioBootstrap 指定 AudioMixer、Music 和 SFX 分组。Mixer 层级为 Master → Music / SFX，分别暴露音量参数 `MasterVolume`、`MusicVolume`、`SfxVolume`，注意大小写。没有 Mixer 时可以播放，但总线音量不会生效。
3. Project → Create → Audio → Audio Cue，添加音频文件。BGM / 引擎循环声勾选 Loop。3D 音效把 Spatial Blend 设为 1，并设置距离；普通 UI 音效使用 Play，始终为 2D。
4. 使用 AudioExample 查看 Inspector 接线方式；游戏组件通过 Initialize 接收 IMusicPlayer / ISfxPlayer。请在 Start 或更晚获取服务，不能在其他组件的字段初始化器中获取。
5. 场景摄像机需要一个启用的 AudioListener；系统不会额外创建监听器。

```csharp
music.Play(levelMusic, fadeSeconds: 1f);
sfx.Play(click);
sfx.PlayAt(explosion, transform.position);
AudioHandle engine = sfx.PlayFollowing(engineLoop, transform);
engine.Pause();
engine.Resume();
engine.Stop(fadeSeconds: 0.3f);
volume.SetVolume(AudioBus.Music, 0.7f);
volume.SetMuted(AudioBus.Master, true);
volume.Save(); // 设置面板关闭时保存；系统也会在应用暂停/退出时保存
```

## 行为约定

- 所有 API 在 Unity 主线程调用。Fade 使用 unscaledDeltaTime；Time.timeScale = 0 不会自动暂停音频。
- 暂停音乐用 Music.Pause/Resume；单个音效用 handle.Pause/Resume；全局暂停可用 AudioListener.pause。
- 淡出停止会在暂停时完成。普通暂停不会被误认为播放结束。场景切换保留 BGM 和普通音效；跟随音效在目标销毁后回收。
- null cue、无有效 clip、失效目标、加载失败、并发达到上限都会返回无效 handle，不抢占已有音效。可用 IsValid 判断请求是否接受。
- AudioCue.MaxInstances 是单个 cue 的音效并发限制；Bootstrap.MaxSfxVoices 是全局限制。暂停中的声音也计入并发。池按需扩容至上限。
- 默认/已完成/已回收 handle 的操作安全无效；代次校验防止旧 handle 停止后来复用的声音。IsValid 表示声音租约有效，包括暂停或等待加载，不等同于正在发声。
- 音乐固定两路；快速连续切歌时复用较安静的一路，从另一条当前音量继续渐变。重复播放当前 cue 不重新开始。BGM 是否循环由 cue 决定。
- 自定义 Output Group 应放在对应 Music 或 SFX 总线下，才能被对应总线音量控制。配置直接引用 AudioClip；第一版不包含 Addressables 加载。
- 音量范围 0–1，转换为 dB 后写入 Mixer；静音保留原音量，0 对应 -80 dB。PlayerPrefs 使用 Smoking.Audio. 前缀，可在 Bootstrap 修改。
- Bootstrap 应始终启用，放在专用根物体上。新增声音只需新建 cue；新增播放策略可以实现对应接口，不需要修改业务组件。

## 验证

Tests/PlayMode 包含对象池句柄复用、暂停、自然结束、跟随目标销毁、并发限制、音乐切换/停止、零音量 BGM、音量保存，以及 prefab 混音器接线/重复启动组件测试。在 Unity Test Runner 的 PlayMode 中运行 Smoking.Audio.Tests。

2026-10-22：Unity 6000.6.0f1 编译通过，11 项 PlayMode 测试全部通过。测试使用生成的静音 clip 验证生命周期；实际素材的听感、响度和空间衰减需要接入后试听。
