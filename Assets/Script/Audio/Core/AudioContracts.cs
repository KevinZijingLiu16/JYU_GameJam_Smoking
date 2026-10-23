using UnityEngine;

namespace Smoking.Audio
{
    public enum AudioBus { Master, Music, Sfx }

    public interface IMusicPlayer
    {
        void Play(AudioCue cue, float fadeSeconds = 1f);
        void Stop(float fadeSeconds = 1f);
        void Pause();
        void Resume();
    }

    public interface ISfxPlayer
    {
        AudioHandle Play(AudioCue cue);
        AudioHandle PlayAt(AudioCue cue, Vector3 position);
        AudioHandle PlayFollowing(AudioCue cue, Transform target);
        void StopAll(float fadeSeconds = 0f);
    }

    public interface IAudioVolumeControl
    {
        float GetVolume(AudioBus bus);
        bool IsMuted(AudioBus bus);
        void SetVolume(AudioBus bus, float volume);
        void SetMuted(AudioBus bus, bool muted);
        void Save();
    }

    internal interface IAudioHandleOwner
    {
        bool IsValid(int slot, ulong version);
        void Stop(int slot, ulong version, float fadeSeconds);
        void SetPaused(int slot, ulong version, bool paused);
    }
}
