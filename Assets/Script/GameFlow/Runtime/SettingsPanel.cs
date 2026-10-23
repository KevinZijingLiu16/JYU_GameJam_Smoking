using Smoking.Audio;
using TMPro;
using UnityEngine;

namespace Smoking.GameFlow
{
    public sealed class SettingsPanel : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Slider musicSlider;
        [SerializeField] private UnityEngine.UI.Slider sfxSlider;
        [SerializeField] private UnityEngine.UI.Slider brightnessSlider;
        [SerializeField] private TMP_Text musicValue;
        [SerializeField] private TMP_Text sfxValue;
        [SerializeField] private TMP_Text brightnessValue;
        [SerializeField] private AudioCue previewSound;
        private GameSettingsService settings;

        private void OnEnable()
        {
            settings = GameSettingsService.Instance;
            if (settings == null) return;
            musicSlider.SetValueWithoutNotify(settings.Volume.GetVolume(AudioBus.Music));
            sfxSlider.SetValueWithoutNotify(settings.Volume.GetVolume(AudioBus.Sfx));
            brightnessSlider.minValue = settings.Config.MinimumBrightness;
            brightnessSlider.maxValue = 1f;
            brightnessSlider.SetValueWithoutNotify(settings.Brightness);
            RefreshLabels();
        }

        public void SetMusic(float value) { settings.Volume.SetVolume(AudioBus.Music, value); RefreshLabels(); }
        public void SetSfx(float value) { settings.Volume.SetVolume(AudioBus.Sfx, value); RefreshLabels(); }
        public void SetBrightness(float value) { settings.SetBrightness(value); RefreshLabels(); }
        public void PreviewSfx() => settings.Sfx.Play(previewSound);
        private void RefreshLabels()
        {
            musicValue.text = Mathf.RoundToInt(musicSlider.value * 100f) + "%";
            sfxValue.text = Mathf.RoundToInt(sfxSlider.value * 100f) + "%";
            brightnessValue.text = Mathf.RoundToInt(brightnessSlider.value * 100f) + "%";
        }
        private void OnDisable() { if (settings != null) settings.Save(); }
    }
}
