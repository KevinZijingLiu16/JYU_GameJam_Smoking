using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Smoking.Audio
{
    public sealed class AudioVolumeController : IAudioVolumeControl
    {
        private readonly AudioMixer mixer;
        private readonly string prefix;
        private readonly string[] parameters;
        private readonly float[] volumes = new float[3];
        private readonly bool[] muted = new bool[3];
        private readonly bool[] warned = new bool[3];

        public AudioVolumeController(AudioMixer mixer, string preferencePrefix = "Smoking.Audio.",
            string masterParameter = "MasterVolume", string musicParameter = "MusicVolume", string sfxParameter = "SfxVolume")
        {
            this.mixer = mixer;
            prefix = preferencePrefix ?? "Smoking.Audio.";
            parameters = new[] { masterParameter, musicParameter, sfxParameter };
            for (int i = 0; i < volumes.Length; i++)
            {
                float saved = PlayerPrefs.GetFloat(Key(i, "Volume"), 1f);
                volumes[i] = float.IsNaN(saved) ? 1f : Mathf.Clamp01(saved);
                muted[i] = PlayerPrefs.GetInt(Key(i, "Muted"), 0) != 0;
            }
        }

        private string Key(int index, string suffix) => prefix + (AudioBus)index + "." + suffix;
        private static int Index(AudioBus bus)
        {
            if (bus < AudioBus.Master || bus > AudioBus.Sfx) throw new ArgumentOutOfRangeException(nameof(bus));
            return (int)bus;
        }

        public float GetVolume(AudioBus bus) => volumes[Index(bus)];
        public bool IsMuted(AudioBus bus) => muted[Index(bus)];
        public void SetVolume(AudioBus bus, float volume)
        {
            if (float.IsNaN(volume) || float.IsInfinity(volume)) throw new ArgumentOutOfRangeException(nameof(volume));
            int index = Index(bus);
            volumes[index] = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(Key(index, "Volume"), volumes[index]);
            Apply(index);
        }

        public void SetMuted(AudioBus bus, bool value)
        {
            int index = Index(bus);
            muted[index] = value;
            PlayerPrefs.SetInt(Key(index, "Muted"), value ? 1 : 0);
            Apply(index);
        }

        /// <summary>Call from Start or later, after Unity initializes the mixer.</summary>
        public void ApplyAll() { for (int i = 0; i < volumes.Length; i++) Apply(i); }
        private void Apply(int index)
        {
            if (mixer == null) return;
            float linear = muted[index] ? 0f : volumes[index];
            float decibels = linear <= 0.0001f ? -80f : 20f * Mathf.Log10(linear);
            if (!mixer.SetFloat(parameters[index], decibels) && !warned[index])
            {
                warned[index] = true;
                Debug.LogWarning($"Audio mixer parameter '{parameters[index]}' is missing. Expose and name the bus volume parameter.", mixer);
            }
        }

        public void Save() => PlayerPrefs.Save();
    }
}
