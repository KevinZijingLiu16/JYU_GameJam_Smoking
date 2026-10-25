using UnityEngine;
using UnityEngine.SceneManagement;

namespace Smoking.Player
{
    // The last checkpoint the player passed. It outlives the scene reload FallRestart does after a
    // fall, so PathWalker can start there instead of at the path start. Only valid in its own scene.
    public static class RespawnPoint
    {
        private static string scene;
        private static Vector3 position;

        public static void Set(Vector3 point)
        {
            scene = SceneManager.GetActiveScene().name;
            position = point;
        }

        // Entering a level any other way (menu, level select, next level, restart) starts it from the beginning.
        public static void Clear() => scene = null;

        public static bool TryGet(out Vector3 point)
        {
            point = position;
            return scene != null && scene == SceneManager.GetActiveScene().name;
        }

        // Statics survive entering Play Mode when domain reload is off.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => Clear();
    }
}
