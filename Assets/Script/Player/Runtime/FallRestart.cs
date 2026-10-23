using System.Collections;
using Smoking.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Smoking.Player
{
    // When the walker runs out of ground: go ragdoll, wait, then reload the scene the player is in.
    public sealed class FallRestart : MonoBehaviour
    {
        [SerializeField] private PathWalker walker;
        [SerializeField] private Ragdoll ragdoll;
        [SerializeField] private float restartDelay = 2.5f;
        [SerializeField] private AudioCue fallSound;

        private void OnEnable() => walker.LostGround += HandleLostGround;
        private void OnDisable() => walker.LostGround -= HandleLostGround;

        private void HandleLostGround()
        {
            ragdoll.Activate(walker.Velocity);
            AudioBootstrap.SharedSfx?.Play(fallSound);
            StartCoroutine(RestartAfterDelay());
        }

        private IEnumerator RestartAfterDelay()
        {
            yield return new WaitForSeconds(restartDelay);
            Time.timeScale = 1f;
            Scene scene = SceneManager.GetActiveScene();
#if UNITY_EDITOR
            // Prototype scenes are often not in the build list yet; load them straight from the asset.
            if (scene.buildIndex < 0)
            {
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scene.path, new LoadSceneParameters(LoadSceneMode.Single));
                yield break;
            }
#endif
            SceneManager.LoadScene(scene.buildIndex);
        }
    }
}
