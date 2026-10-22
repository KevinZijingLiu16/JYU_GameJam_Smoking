using UnityEngine;

namespace Smoking.Audio.Examples
{
    /// <summary>Scene composition example. Public methods can also be wired to UnityEvents.</summary>
    public sealed class AudioExample : MonoBehaviour
    {
        [SerializeField] private AudioBootstrap audioRoot;
        [SerializeField] private AudioCue backgroundMusic;
        [SerializeField] private AudioCue click;
        [SerializeField] private AudioCue positionalEffect;
        [SerializeField] private AudioCue followingLoop;

        private IMusicPlayer music;
        private ISfxPlayer sfx;
        private AudioHandle loop;

        private void Start()
        {
            if (audioRoot == null)
            {
                Debug.LogWarning("AudioExample needs an AudioBootstrap reference.", this);
                return;
            }
            Initialize(audioRoot.Music, audioRoot.Sfx);
            music?.Play(backgroundMusic);
        }

        // Other gameplay classes can use this pattern without knowing AudioBootstrap.
        public void Initialize(IMusicPlayer musicPlayer, ISfxPlayer sfxPlayer)
        { music = musicPlayer; sfx = sfxPlayer; }

        public void PlayClick() => sfx?.Play(click);
        public void PlayEffect() => sfx?.PlayAt(positionalEffect, transform.position);
        public void StartLoop()
        {
            if (!loop.IsValid && sfx != null) loop = sfx.PlayFollowing(followingLoop, transform);
        }
        public void StopLoop() => loop.Stop(0.2f);
        private void OnDisable() => loop.Stop();
    }
}
