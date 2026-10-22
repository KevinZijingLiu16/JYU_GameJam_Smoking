using UnityEngine;
using UnityEngine.Audio;

namespace Smoking.Audio
{
    [CreateAssetMenu(menuName = "Audio/Audio Cue", fileName = "NewAudioCue")]
    public sealed class AudioCue : ScriptableObject
    {
        [SerializeField] private AudioClip[] clips = new AudioClip[0];
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private Vector2 volumeVariation = Vector2.one;
        [SerializeField] private Vector2 pitchRange = Vector2.one;
        [SerializeField] private bool loop;
        [SerializeField, Range(0f, 1f)] private float spatialBlend;
        [SerializeField, Min(0.01f)] private float minDistance = 1f;
        [SerializeField, Min(0.01f)] private float maxDistance = 30f;
        [SerializeField] private AudioRolloffMode rolloff = AudioRolloffMode.Logarithmic;
        [Tooltip("Leave empty to use the player's Music or SFX group. Custom groups should be children of that bus.")]
        [SerializeField] private AudioMixerGroup outputGroup;
        [Tooltip("SFX only. When full, new requests are rejected; existing sounds keep playing.")]
        [SerializeField, Min(1)] private int maxInstances = 8;

        public int MaxInstances => Mathf.Max(1, maxInstances);

        internal bool TrySample(out AudioClip clip, out float gain, out float pitch)
        {
            clip = null;
            gain = Mathf.Clamp01(volume * Random.Range(volumeVariation.x, volumeVariation.y));
            pitch = Mathf.Clamp(Random.Range(pitchRange.x, pitchRange.y), 0.01f, 3f);
            // Reservoir sampling ignores unassigned entries without allocating a temporary array.
            int count = 0;
            if (clips != null)
                foreach (var candidate in clips)
                    if (candidate != null && Random.Range(0, ++count) == 0) clip = candidate;
            return clip != null;
        }

        internal void Configure(AudioSource source, AudioClip clip, float gain, float pitch,
            AudioMixerGroup fallbackGroup, bool force2D)
        {
            source.Stop();
            source.playOnAwake = false;
            source.clip = clip;
            source.volume = gain;
            source.pitch = pitch;
            source.loop = loop;
            source.outputAudioMixerGroup = outputGroup != null ? outputGroup : fallbackGroup;
            source.spatialBlend = force2D ? 0f : spatialBlend;
            source.minDistance = Mathf.Max(0.01f, minDistance);
            source.maxDistance = Mathf.Max(source.minDistance, maxDistance);
            source.rolloffMode = rolloff;
            source.dopplerLevel = 0f;
        }

        private void OnValidate()
        {
            volumeVariation.x = Mathf.Clamp01(volumeVariation.x);
            volumeVariation.y = Mathf.Clamp(volumeVariation.y, volumeVariation.x, 1f);
            pitchRange.x = Mathf.Clamp(pitchRange.x, 0.01f, 3f);
            pitchRange.y = Mathf.Clamp(pitchRange.y, pitchRange.x, 3f);
            maxDistance = Mathf.Max(minDistance, maxDistance);
        }
    }
}
