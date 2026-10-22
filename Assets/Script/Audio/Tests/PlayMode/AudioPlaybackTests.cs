using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Smoking.Audio.Tests
{
    public sealed class AudioPlaybackTests
    {
        private GameObject root;
        private AudioSourcePool pool;
        private SfxPlayer sfx;
        private MusicPlayer music;
        private readonly List<UnityEngine.Object> assets = new List<UnityEngine.Object>();

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Audio Test Root");
            pool = new AudioSourcePool(root.transform, null, 2, 1);
            sfx = new SfxPlayer(pool);
            music = new MusicPlayer(root.transform, null);
        }

        private AudioCue Cue(bool loop = true, int maxInstances = 8, float duration = 2f)
        {
            var cue = ScriptableObject.CreateInstance<AudioCue>();
            var clip = AudioClip.Create("Test silence", (int)(44100 * duration), 1, 44100, false);
            assets.Add(cue);
            assets.Add(clip);
            Set(cue, "clips", new[] { clip });
            Set(cue, "loop", loop);
            Set(cue, "maxInstances", maxInstances);
            return cue;
        }

        private static void Set(object obj, string field, object value) => obj.GetType()
            .GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(obj, value);

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            AudioListener.pause = false;
            pool.Dispose();
            music.Dispose();
            UnityEngine.Object.Destroy(root);
            foreach (var asset in assets) UnityEngine.Object.Destroy(asset);
            assets.Clear();
            yield return null;
        }

        [Test]
        public void RecycledHandleCannotStopNewPlayback()
        {
            var cue = Cue();
            var old = sfx.Play(cue);
            Assert.That(old.IsValid, Is.True);
            old.Stop();
            var next = sfx.Play(cue);
            old.Stop(); old.Pause(); old.Resume();
            Assert.That(old.IsValid, Is.False);
            Assert.That(next.IsValid, Is.True);
            Assert.That(pool.ActiveCount, Is.EqualTo(1));
        }

        [Test]
        public void LimitsRejectNewRequestsWithoutStealing()
        {
            var limited = Cue(maxInstances: 1);
            var first = sfx.Play(limited);
            Assert.That(sfx.Play(limited).IsValid, Is.False);
            var second = sfx.Play(Cue());
            Assert.That(sfx.Play(Cue()).IsValid, Is.False);
            Assert.That(first.IsValid && second.IsValid, Is.True);
        }

        [Test]
        public void InvalidRequestsAndDefaultHandlesAreSafe()
        {
            AudioHandle empty = default;
            empty.Stop(); empty.Pause(); empty.Resume();
            Assert.That(empty.IsValid, Is.False);
            Assert.That(sfx.Play(null).IsValid, Is.False);
            var cue = ScriptableObject.CreateInstance<AudioCue>(); assets.Add(cue);
            Assert.That(sfx.Play(cue).IsValid, Is.False);
            Assert.That(sfx.PlayFollowing(Cue(), null).IsValid, Is.False);
        }

        [UnityTest]
        public IEnumerator PausedVoiceSurvivesTickAndCanFadeOut()
        {
            var handle = sfx.Play(Cue());
            yield return null;
            handle.Pause();
            pool.Tick(1f);
            Assert.That(handle.IsValid, Is.True);
            handle.Resume();
            AudioListener.pause = true;
            pool.Tick(1f);
            Assert.That(handle.IsValid, Is.True);
            handle.Stop(0.25f);
            pool.Tick(0.3f);
            Assert.That(handle.IsValid, Is.False);
        }

        [UnityTest]
        public IEnumerator CompletedOneShotReturnsToPool()
        {
            var handle = sfx.Play(Cue(false, duration: 0.05f));
            yield return new WaitForSecondsRealtime(0.3f);
            pool.Tick(0.3f);
            Assert.That(handle.IsValid, Is.False);
            Assert.That(pool.ActiveCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator FollowingVoiceTracksAndReleasesDestroyedTarget()
        {
            var target = new GameObject("Follow target");
            var handle = sfx.PlayFollowing(Cue(), target.transform);
            target.transform.position = new Vector3(2f, 3f, 4f);
            pool.Tick(0f);
            Assert.That(root.transform.Find("SFX Voices/Voice 0").position, Is.EqualTo(target.transform.position));
            UnityEngine.Object.Destroy(target);
            yield return null;
            pool.Tick(0.1f);
            Assert.That(handle.IsValid, Is.False);
        }

        [UnityTest]
        public IEnumerator RapidMusicChangesRemainBoundedAndStopWhilePaused()
        {
            music.Play(Cue(), 0f);
            yield return null;
            music.Play(Cue(), 1f);
            music.Tick(0.2f);
            music.Play(Cue(), 1f);
            music.Tick(0.2f);
            var sources = root.transform.Find("Music Voices").GetComponentsInChildren<AudioSource>();
            Assert.That(sources.Length, Is.EqualTo(2));
            Assert.That(Array.Exists(sources, source => source.clip != null), Is.True);
            music.Pause();
            music.Stop(0.2f);
            music.Tick(0.3f);
            foreach (var source in sources) Assert.That(source.clip, Is.Null);
        }

        [Test]
        public void VolumeSettingsRoundTripAndMutePreservesVolume()
        {
            string prefix = "Smoking.Audio.Test." + Guid.NewGuid() + ".";
            try
            {
                var first = new AudioVolumeController(null, prefix);
                first.SetVolume(AudioBus.Music, 0.35f);
                first.SetMuted(AudioBus.Music, true);
                first.Save();
                var restored = new AudioVolumeController(null, prefix);
                Assert.That(restored.GetVolume(AudioBus.Music), Is.EqualTo(0.35f).Within(0.001f));
                Assert.That(restored.IsMuted(AudioBus.Music), Is.True);
                restored.SetMuted(AudioBus.Music, false);
                Assert.That(restored.GetVolume(AudioBus.Music), Is.EqualTo(0.35f).Within(0.001f));
            }
            finally
            {
                PlayerPrefs.DeleteKey(prefix + "Music.Volume");
                PlayerPrefs.DeleteKey(prefix + "Music.Muted");
                PlayerPrefs.Save();
            }
        }

        [Test]
        public void SilentMusicCueCanPlayAndStopImmediately()
        {
            var cue = Cue();
            Set(cue, "volume", 0f);
            Assert.DoesNotThrow(() => music.Play(cue, 0f));
            music.Tick(0.1f);
            var sources = root.transform.Find("Music Voices").GetComponentsInChildren<AudioSource>();
            Assert.That(Array.Exists(sources, source => source.clip != null), Is.True);
            music.Stop(0f);
            foreach (var source in sources) Assert.That(source.clip, Is.Null);
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator PrefabRoutesMixerAndDuplicateSceneRootSharesServices()
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Script/Audio/Setup/AudioBootstrap.prefab");
            Assert.That(prefab, Is.Not.Null);
            var original = UnityEngine.Object.Instantiate(prefab);
            assets.Add(original);
            var bootstrap = original.GetComponent<AudioBootstrap>();
            yield return null; // Start applies mixer values after initialization.
            var duplicate = UnityEngine.Object.Instantiate(prefab);
            assets.Add(duplicate);
            var other = duplicate.GetComponent<AudioBootstrap>();
            Assert.That(other.Music, Is.SameAs(bootstrap.Music));
            Assert.That(other.Sfx, Is.SameAs(bootstrap.Sfx));
            Assert.That(duplicate.GetComponentsInChildren<AudioSource>(), Is.Empty);
            Assert.That(original.scene.name, Is.EqualTo("DontDestroyOnLoad"));

            var mixer = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Audio.AudioMixer>(
                "Assets/Script/Audio/Setup/GameAudio.mixer");
            foreach (string parameter in new[] { "MasterVolume", "MusicVolume", "SfxVolume" })
                Assert.That(mixer.GetFloat(parameter, out _), Is.True, parameter);
            var controller = new AudioVolumeController(mixer, "Smoking.Audio.MixerTest.");
            try
            {
                controller.SetVolume(AudioBus.Music, 0.5f);
                Assert.That(mixer.GetFloat("MusicVolume", out float db), Is.True);
                Assert.That(db, Is.EqualTo(-6.0206f).Within(0.01f));
                controller.SetMuted(AudioBus.Music, true);
                mixer.GetFloat("MusicVolume", out db);
                Assert.That(db, Is.EqualTo(-80f));
            }
            finally
            {
                PlayerPrefs.DeleteKey("Smoking.Audio.MixerTest.Music.Volume");
                PlayerPrefs.DeleteKey("Smoking.Audio.MixerTest.Music.Muted");
                var actual = bootstrap.Volume;
                actual.SetVolume(AudioBus.Music, actual.GetVolume(AudioBus.Music));
                actual.Save();
            }
            bootstrap.Music.Play(Cue(), 0f);
            bootstrap.Sfx.Play(Cue());
            foreach (var source in original.GetComponentsInChildren<AudioSource>())
                Assert.That(source.outputAudioMixerGroup != null || source.clip == null, Is.True);
            UnityEngine.Object.Destroy(duplicate);
            yield return null;
            Assert.That(bootstrap.Music, Is.Not.Null);
        }
#endif

        [Test]
        public void DisposalInvalidatesOutstandingHandles()
        {
            var handle = sfx.Play(Cue());
            pool.Dispose();
            Assert.That(handle.IsValid, Is.False);
            handle.Stop();
            Assert.That(sfx.Play(Cue()).IsValid, Is.False);
        }
    }
}
