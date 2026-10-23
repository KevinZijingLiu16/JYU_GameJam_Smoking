using System.Collections;
using NUnit.Framework;
using Smoking.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Smoking.GameFlow.Tests
{
    public sealed class GameFlowTests
    {
        private float originalMusic, originalSfx, originalBrightness;
        private bool hadBrightness;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            hadBrightness = PlayerPrefs.HasKey("Smoking.Display.Brightness");
            originalBrightness = PlayerPrefs.GetFloat("Smoking.Display.Brightness", 1f);
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;
            originalMusic = GameSettingsService.Instance.Volume.GetVolume(AudioBus.Music);
            originalSfx = GameSettingsService.Instance.Volume.GetVolume(AudioBus.Sfx);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1f;
            var service = GameSettingsService.Instance;
            if (service != null)
            {
                service.Volume.SetVolume(AudioBus.Music, originalMusic);
                service.Volume.SetVolume(AudioBus.Sfx, originalSfx);
                service.SetBrightness(originalBrightness);
                service.Save();
                Object.Destroy(service.gameObject);
            }
            if (!hadBrightness) PlayerPrefs.DeleteKey("Smoking.Display.Brightness");
            foreach (var audio in Object.FindObjectsByType<AudioBootstrap>(FindObjectsSortMode.None)) Object.Destroy(audio.gameObject);
            yield return null;
            PlayerPrefs.Save();
        }

        private static void Click(string name)
        {
            var go = GameObject.Find(name);
            Assert.That(go, Is.Not.Null, name);
            go.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        }

        private static IEnumerator WaitForScene(string name)
        {
            float deadline = Time.realtimeSinceStartup + 10f;
            while (SceneManager.GetActiveScene().name != name && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(name));
            yield return null;
        }

        [UnityTest]
        public IEnumerator SettingsApplyAcrossScenesAndClampBrightness()
        {
            Click("SettingsButton");
            var settings = GameSettingsService.Instance;
            GameObject.Find("MusicSlider").GetComponent<UnityEngine.UI.Slider>().value = .42f;
            GameObject.Find("SfxSlider").GetComponent<UnityEngine.UI.Slider>().value = .23f;
            settings.SetBrightness(0f);
            Assert.That(settings.Brightness, Is.EqualTo(settings.Config.MinimumBrightness));
            Assert.That(GameObject.Find("BrightnessShade").GetComponent<UnityEngine.UI.Image>().color.a,
                Is.EqualTo(1f - settings.Config.MinimumBrightness).Within(.001f));
            Click("BackButton");
            Click("StartButton");
            yield return WaitForScene("Gameplay");
            Assert.That(GameSettingsService.Instance, Is.SameAs(settings));
            Assert.That(settings.Volume.GetVolume(AudioBus.Music), Is.EqualTo(.42f).Within(.001f));
            Assert.That(settings.Volume.GetVolume(AudioBus.Sfx), Is.EqualTo(.23f).Within(.001f));
            Assert.That(GameObject.Find("BrightnessShade").GetComponent<UnityEngine.UI.Image>().color.a,
                Is.EqualTo(1f - settings.Config.MinimumBrightness).Within(.001f));
            Assert.That(Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator EscapePausesClockAndSettingsBackKeepsPause()
        {
            Click("StartButton");
            yield return WaitForScene("Gameplay");
            var menu = Object.FindFirstObjectByType<MenuPanels>();
            var clock = Object.FindFirstObjectByType<GameplayClock>();
            menu.HandleEscape();
            yield return null;
            Assert.That(Time.timeScale, Is.Zero);
            float elapsed = clock.Elapsed;
            yield return new WaitForSecondsRealtime(.1f);
            Assert.That(clock.Elapsed, Is.EqualTo(elapsed));
            Click("SettingsButton");
            menu.HandleEscape();
            Assert.That(menu.IsPaused, Is.True);
            Assert.That(GameObject.Find("PausePanel"), Is.Not.Null);
            menu.HandleEscape();
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            yield return new WaitForSecondsRealtime(.1f);
            Assert.That(clock.Elapsed, Is.GreaterThan(elapsed));
            menu.Pause();
            Click("MainMenuButton");
            yield return WaitForScene("MainMenu");
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator EscapeKeyboardInputOpensPauseMenu()
        {
            Click("StartButton");
            yield return WaitForScene("Gameplay");
            var inputSettings = UnityEngine.InputSystem.InputSystem.settings;
            var originalBehavior = inputSettings.editorInputBehaviorInPlayMode;
            var originalBackground = inputSettings.backgroundBehavior;
            UnityEngine.InputSystem.Keyboard keyboard = null;
            try
            {
                // Route synthetic keys into the player even when Test Runner has editor focus.
                inputSettings.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                inputSettings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
                keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,
                    new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.Escape));
                yield return null;
                yield return null;
                Assert.That(Object.FindFirstObjectByType<MenuPanels>().IsPaused, Is.True,
                    $"Injected key held={keyboard.escapeKey.isPressed}; current={UnityEngine.InputSystem.Keyboard.current == keyboard}; frame={Time.frameCount}");
                Assert.That(Time.timeScale, Is.Zero);
            }
            finally
            {
                if (keyboard != null) UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
                inputSettings.editorInputBehaviorInPlayMode = originalBehavior;
                inputSettings.backgroundBehavior = originalBackground;
            }
        }

        [UnityTest]
        public IEnumerator RestartAndCreditsNavigationWork()
        {
            Click("RestartButton");
            yield return WaitForScene("Gameplay");
            Click("CompleteButton");
            yield return WaitForScene("Credits");
            var rect = GameObject.Find("CreditsText").GetComponent<RectTransform>();
            float initial = rect.anchoredPosition.y;
            yield return new WaitForSecondsRealtime(1.25f);
            Assert.That(rect.anchoredPosition.y, Is.GreaterThan(initial));
            Click("MainMenuButton");
            yield return WaitForScene("MainMenu");
        }
    }
}
