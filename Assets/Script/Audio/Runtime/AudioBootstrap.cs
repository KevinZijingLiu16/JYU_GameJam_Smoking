using UnityEngine;
using UnityEngine.Audio;

namespace Smoking.Audio
{
    /// <summary>Composition root; consumers receive the narrow interfaces they need.</summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class AudioBootstrap : MonoBehaviour
    {
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private AudioMixerGroup musicGroup;
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField, Min(1)] private int maxSfxVoices = 32;
        [SerializeField, Min(0)] private int prewarmVoices = 8;
        [SerializeField] private string preferencePrefix = "Smoking.Audio.";
        [SerializeField] private string masterParameter = "MasterVolume";
        [SerializeField] private string musicParameter = "MusicVolume";
        [SerializeField] private string sfxParameter = "SfxVolume";

        private static AudioBootstrap active;
        private AudioSourcePool pool;
        private MusicPlayer music;
        private SfxPlayer sfx;
        private AudioVolumeController volume;

        public IMusicPlayer Music => active != null ? active.music : null;
        public ISfxPlayer Sfx => active != null ? active.sfx : null;
        public IAudioVolumeControl Volume => active != null ? active.volume : null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => active = null;

        private void Awake()
        {
            if (active != null && active != this)
            {
                // Keep this lightweight scene reference so consumers wired in the new scene
                // can resolve the persistent services. No additional voices are created.
                enabled = false;
                return;
            }
            active = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            pool = new AudioSourcePool(transform, sfxGroup, maxSfxVoices, prewarmVoices);
            music = new MusicPlayer(transform, musicGroup);
            sfx = new SfxPlayer(pool);
            volume = new AudioVolumeController(mixer, preferencePrefix, masterParameter, musicParameter, sfxParameter);
        }

        private void Start()
        {
            if (mixer == null || musicGroup == null || sfxGroup == null)
                Debug.LogWarning("AudioBootstrap: assign a mixer and Music/SFX groups to enable bus volume controls.", this);
            volume.ApplyAll();
        }

        private void Update()
        {
            pool.Tick(Time.unscaledDeltaTime);
            music.Tick(Time.unscaledDeltaTime);
        }

        private void OnApplicationPause(bool paused) { if (paused) volume?.Save(); }
        private void OnApplicationQuit() => volume?.Save();
        private void OnDestroy()
        {
            if (active != this) return;
            pool?.Dispose();
            music?.Dispose();
            volume?.Save();
            active = null;
        }
    }
}
