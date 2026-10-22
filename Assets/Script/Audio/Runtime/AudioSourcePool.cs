using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Smoking.Audio
{
    /// <summary>Bounded SFX voices. Tick on Unity's main thread with unscaled delta time.</summary>
    public sealed class AudioSourcePool : IDisposable, IAudioHandleOwner
    {
        private sealed class Voice
        {
            public AudioSource Source;
            public AudioCue Cue;
            public Transform Target;
            public ulong Version;
            public bool Active, Following, Paused, Started, Fading;
            public float FadeDuration, FadeElapsed, FadeStart;
        }

        private readonly List<Voice> voices = new List<Voice>();
        private readonly Transform root;
        private readonly AudioMixerGroup group;
        private readonly int capacity;
        private bool disposed;

        public AudioSourcePool(Transform parent, AudioMixerGroup group, int capacity = 32, int prewarm = 8)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            this.group = group;
            this.capacity = Mathf.Max(1, capacity);
            root = new GameObject("SFX Voices").transform;
            root.SetParent(parent, false);
            for (int i = 0; i < Mathf.Clamp(prewarm, 0, this.capacity); i++) AddVoice();
        }

        public int ActiveCount
        {
            get { int count = 0; foreach (var voice in voices) if (voice.Active) count++; return count; }
        }

        private Voice AddVoice()
        {
            var child = new GameObject($"Voice {voices.Count}");
            child.transform.SetParent(root, false);
            var voice = new Voice { Source = child.AddComponent<AudioSource>() };
            voice.Source.playOnAwake = false;
            voices.Add(voice);
            return voice;
        }

        internal AudioHandle Play(AudioCue cue, Vector3 position, Transform target, bool following, bool force2D)
        {
            if (disposed || cue == null || (following && target == null)) return default;
            int instances = 0, free = -1;
            for (int i = 0; i < voices.Count; i++)
            {
                if (voices[i].Active && voices[i].Cue == cue) instances++;
                if (!voices[i].Active && free < 0) free = i;
            }
            if (instances >= cue.MaxInstances || (free < 0 && voices.Count >= capacity)) return default;
            if (!cue.TrySample(out var clip, out var gain, out var pitch)) return default;
            if (clip.loadState == AudioDataLoadState.Failed) return default;
            if (free < 0) { free = voices.Count; AddVoice(); }
            var voice = voices[free];
            voice.Version++;
            voice.Active = true;
            voice.Cue = cue;
            voice.Target = target;
            voice.Following = following;
            voice.Paused = voice.Started = voice.Fading = false;
            voice.Source.transform.position = position;
            cue.Configure(voice.Source, clip, gain, pitch, group, force2D);
            if (clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
            TryStart(voice);
            return new AudioHandle(this, free, voice.Version);
        }

        private static void TryStart(Voice voice)
        {
            if (!voice.Started && !voice.Paused && voice.Source.clip.loadState == AudioDataLoadState.Loaded)
            {
                voice.Source.Play();
                voice.Started = true;
            }
        }

        public void Tick(float deltaTime)
        {
            if (disposed) return;
            foreach (var voice in voices)
            {
                if (!voice.Active) continue;
                if (voice.Source == null || voice.Cue == null || (voice.Following && voice.Target == null))
                { Release(voice); continue; }
                if (voice.Following) voice.Source.transform.position = voice.Target.position;
                // Explicit stops must finish even while paused.
                if (voice.Fading)
                {
                    voice.FadeElapsed += Mathf.Max(0f, deltaTime);
                    voice.Source.volume = voice.FadeStart * (1f - Mathf.Clamp01(voice.FadeElapsed / voice.FadeDuration));
                    if (voice.FadeElapsed >= voice.FadeDuration) { Release(voice); continue; }
                }
                if (voice.Paused || AudioListener.pause) continue;
                if (voice.Source.clip == null || voice.Source.clip.loadState == AudioDataLoadState.Failed)
                { Release(voice); continue; }
                if (!voice.Started) { TryStart(voice); continue; }
                if (!voice.Source.isPlaying) Release(voice);
            }
        }

        private static void Release(Voice voice)
        {
            if (voice.Source != null) { voice.Source.Stop(); voice.Source.clip = null; }
            voice.Active = false;
            voice.Cue = null;
            voice.Target = null;
        }

        bool IAudioHandleOwner.IsValid(int slot, ulong version) => IsValid(slot, version);
        private bool IsValid(int slot, ulong version) => !disposed && slot >= 0 && slot < voices.Count
            && voices[slot].Active && voices[slot].Version == version && voices[slot].Source != null;

        void IAudioHandleOwner.Stop(int slot, ulong version, float fadeSeconds)
        {
            if (!IsValid(slot, version)) return;
            var voice = voices[slot];
            if (!(fadeSeconds > 0f) || float.IsInfinity(fadeSeconds)) { Release(voice); return; }
            voice.Fading = true;
            voice.FadeStart = voice.Source.volume;
            voice.FadeElapsed = 0f;
            voice.FadeDuration = fadeSeconds;
        }

        void IAudioHandleOwner.SetPaused(int slot, ulong version, bool paused)
        {
            if (!IsValid(slot, version)) return;
            var voice = voices[slot];
            if (voice.Paused == paused) return;
            voice.Paused = paused;
            if (paused) voice.Source.Pause();
            else if (voice.Started) voice.Source.UnPause();
            else TryStart(voice);
        }

        public void StopAll(float fadeSeconds = 0f)
        {
            for (int i = 0; i < voices.Count; i++)
                ((IAudioHandleOwner)this).Stop(i, voices[i].Version, fadeSeconds);
        }

        public void Dispose()
        {
            if (disposed) return;
            StopAll();
            disposed = true;
            if (root != null) UnityEngine.Object.Destroy(root.gameObject);
        }
    }
}
