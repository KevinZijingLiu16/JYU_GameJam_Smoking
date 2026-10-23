using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Smoking.Player
{
    // When the walker runs out of ground: go ragdoll, wait, then restart the level.
    public sealed class FallRestart : MonoBehaviour
    {
        [SerializeField] private PathWalker walker;
        [SerializeField] private Ragdoll ragdoll;
        [SerializeField] private float restartDelay = 2.5f;
        [Tooltip("Restart hook, e.g. SceneFlow.RestartGame. Empty = reload the active scene.")]
        [SerializeField] private UnityEvent restart;

        private void OnEnable() => walker.LostGround += HandleLostGround;
        private void OnDisable() => walker.LostGround -= HandleLostGround;

        private void HandleLostGround()
        {
            ragdoll.Activate(walker.Velocity);
            StartCoroutine(RestartAfterDelay());
        }

        private IEnumerator RestartAfterDelay()
        {
            yield return new WaitForSeconds(restartDelay);
            if (restart.GetPersistentEventCount() > 0) restart.Invoke();
            else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
