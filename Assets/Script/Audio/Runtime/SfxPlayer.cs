using System;
using UnityEngine;

namespace Smoking.Audio
{
    public sealed class SfxPlayer : ISfxPlayer
    {
        private readonly AudioSourcePool pool;
        public SfxPlayer(AudioSourcePool pool) => this.pool = pool ?? throw new ArgumentNullException(nameof(pool));
        /// <summary>Non-positional/UI sound; always 2D.</summary>
        public AudioHandle Play(AudioCue cue) => pool.Play(cue, Vector3.zero, null, false, true);
        public AudioHandle PlayAt(AudioCue cue, Vector3 position) => pool.Play(cue, position, null, false, false);
        public AudioHandle PlayFollowing(AudioCue cue, Transform target) => target == null ? default
            : pool.Play(cue, target.position, target, true, false);
        public void StopAll(float fadeSeconds = 0f) => pool.StopAll(fadeSeconds);
    }
}
