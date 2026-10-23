using System.Collections;
using Smoking.Audio;
using UnityEngine;
using UnityEngine.Events;

namespace Smoking.GameFlow
{
    // The end of a level. When the player reaches it: fade to a title card ("LEVEL 2"),
    // then load the next scene in GameSettings' level order (the credits after the last level).
    public sealed class LevelExit : PlayerZone
    {
        [SerializeField] private SceneFlow flow;
        [Tooltip("Left empty: \"LEVEL n\" for the next level, \"THE END\" before the credits.")]
        [SerializeField] private string heading;
        [SerializeField] private string caption;
        [SerializeField] private float fadeTime = 0.8f;
        [SerializeField] private float holdTime = 1.6f;
        [SerializeField] private AudioCue completedSound;
        [Tooltip("Runs the moment the player arrives, e.g. disable the PathWalker so they stop walking.")]
        [SerializeField] private UnityEvent reached;

        private bool done;

        protected override Color GizmoColor => new Color(0.95f, 0.75f, 0.3f, 0.9f);

        protected override void Start()
        {
            base.Start();
            if (flow == null) flow = FindFirstObjectByType<SceneFlow>();
        }

        protected override void OnPlayerEnter()
        {
            if (done) return;
            done = true;
            reached?.Invoke();
            GameSettingsService.Instance.Sfx.Play(completedSound);
            StartCoroutine(Finish());
        }

        private IEnumerator Finish()
        {
            yield return LevelOverlay.Instance.PlayTransition(Heading(), caption, fadeTime, holdTime);
            flow.CompleteGame();
        }

        private string Heading()
        {
            if (!string.IsNullOrEmpty(heading)) return heading;
            var config = GameSettingsService.Instance.Config;
            int index = System.Array.IndexOf(config.levelScenes, flow.NextScene);
            return index >= 0 ? $"LEVEL {index + 1}" : "THE END";
        }
    }
}
