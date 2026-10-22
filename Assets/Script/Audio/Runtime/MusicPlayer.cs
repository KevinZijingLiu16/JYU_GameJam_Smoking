using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Smoking.Audio
{
    public sealed class MusicPlayer : IMusicPlayer, IDisposable
    {
        private sealed class Track
        {
            public AudioSource Source;
            public bool Active, Started, Stopping;
            public float From, To, Elapsed, Duration;
        }

        private readonly Track[] tracks = new Track[2];
        private readonly AudioMixerGroup group;
        private readonly Transform root;
        private bool paused, disposed;
        private int current = -1;
        private AudioCue currentCue;

        public MusicPlayer(Transform parent, AudioMixerGroup group)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            this.group = group;
            root = new GameObject("Music Voices").transform;
            root.SetParent(parent, false);
            for (int i = 0; i < tracks.Length; i++)
            {
                var child = new GameObject($"Music {i}");
                child.transform.SetParent(root, false);
                tracks[i] = new Track { Source = child.AddComponent<AudioSource>() };
                tracks[i].Source.playOnAwake = false;
            }
        }

        public void Play(AudioCue cue, float fadeSeconds = 1f)
        {
            if (disposed || cue == null) return;
            if (currentCue == cue && current >= 0 && tracks[current].Active) return;
            if (!cue.TrySample(out var clip, out var gain, out var pitch) || clip.loadState == AudioDataLoadState.Failed) return;
            int next = !tracks[0].Active ? 0 : !tracks[1].Active ? 1
                : tracks[0].Source.volume <= tracks[1].Source.volume ? 0 : 1;
            var incoming = tracks[next];
            Release(incoming);
            cue.Configure(incoming.Source, clip, 0f, pitch, group, true);
            incoming.Active = true;
            incoming.Stopping = false;
            if (clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
            SetFade(incoming, gain, fadeSeconds);
            for (int i = 0; i < tracks.Length; i++)
                if (i != next && tracks[i].Active) BeginStop(tracks[i], fadeSeconds);
            current = next;
            currentCue = cue;
            TryStart(incoming);
        }

        private void TryStart(Track track)
        {
            if (!paused && !track.Started && track.Source.clip.loadState == AudioDataLoadState.Loaded)
            { track.Source.Play(); track.Started = true; }
        }

        private static void SetFade(Track track, float target, float seconds)
        {
            track.From = track.Source.volume;
            track.To = target;
            track.Elapsed = 0f;
            track.Duration = float.IsNaN(seconds) || float.IsInfinity(seconds) ? 0f : Mathf.Max(0f, seconds);
            if (track.Duration == 0f)
            {
                track.Source.volume = target;
                if (track.Stopping) Release(track);
            }
        }

        private static void BeginStop(Track track, float seconds)
        {
            track.Stopping = true;
            SetFade(track, 0f, seconds);
        }

        public void Tick(float deltaTime)
        {
            if (disposed) return;
            foreach (var track in tracks)
            {
                if (!track.Active) continue;
                if (track.Source.clip == null || track.Source.clip.loadState == AudioDataLoadState.Failed)
                { Release(track); continue; }
                // Fading out is also a stop request and completes while music is paused.
                if (track.Stopping || (!paused && !AudioListener.pause && track.Started))
                {
                    track.Elapsed += Mathf.Max(0f, deltaTime);
                    float t = track.Duration <= 0f ? 1f : Mathf.Clamp01(track.Elapsed / track.Duration);
                    track.Source.volume = Mathf.Lerp(track.From, track.To, t);
                    if (t >= 1f && track.Stopping) { Release(track); continue; }
                }
                if (paused || AudioListener.pause) continue;
                if (!track.Started) { TryStart(track); continue; }
                if (!track.Source.isPlaying) Release(track);
            }
        }

        public void Stop(float fadeSeconds = 1f)
        {
            if (disposed) return;
            foreach (var track in tracks) if (track.Active) BeginStop(track, fadeSeconds);
            currentCue = null;
            current = -1;
        }

        public void Pause()
        {
            if (disposed || paused) return;
            paused = true;
            foreach (var track in tracks) if (track.Active && track.Started) track.Source.Pause();
        }

        public void Resume()
        {
            if (disposed || !paused) return;
            paused = false;
            foreach (var track in tracks)
                if (track.Active) { if (track.Started) track.Source.UnPause(); else TryStart(track); }
        }

        private static void Release(Track track)
        {
            track.Source.Stop();
            track.Source.clip = null;
            track.Active = track.Started = false;
        }

        public void Dispose()
        {
            if (disposed) return;
            Stop(0f);
            disposed = true;
            if (root != null) UnityEngine.Object.Destroy(root.gameObject);
        }
    }
}
