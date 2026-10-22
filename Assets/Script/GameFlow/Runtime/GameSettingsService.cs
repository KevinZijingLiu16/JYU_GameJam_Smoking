using System;
using Smoking.Audio;
using UnityEngine;

namespace Smoking.GameFlow
{
    [DefaultExecutionOrder(-900)]
    public sealed class GameSettingsService : MonoBehaviour
    {
        private const string BrightnessKey = "Smoking.Display.Brightness";
        [SerializeField] private GameSettingsConfig config;
        [SerializeField] private AudioBootstrap audioRoot;
        public static GameSettingsService Instance { get; private set; }
        public GameSettingsConfig Config => config;
        public IAudioVolumeControl Volume => audioRoot.Volume;
        public IMusicPlayer Music => audioRoot.Music;
        public ISfxPlayer Sfx => audioRoot.Sfx;
        public float Brightness { get; private set; }
        public event Action<float> BrightnessChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            float saved = PlayerPrefs.GetFloat(BrightnessKey, config.defaultBrightness);
            Brightness = Mathf.Clamp(float.IsNaN(saved) ? config.defaultBrightness : saved, config.MinimumBrightness, 1f);
        }

        public void SetBrightness(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return;
            Brightness = Mathf.Clamp(value, config.MinimumBrightness, 1f);
            PlayerPrefs.SetFloat(BrightnessKey, Brightness);
            BrightnessChanged?.Invoke(Brightness);
        }

        public void Save() { Volume?.Save(); PlayerPrefs.Save(); }
        private void OnApplicationPause(bool paused) { if (paused && Instance == this) Save(); }
        private void OnApplicationQuit() { if (Instance == this) Save(); }
        private void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
