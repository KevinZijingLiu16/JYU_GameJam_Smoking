using Smoking.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Smoking.GameFlow
{
    public sealed class SceneFlow : MonoBehaviour
    {
        [SerializeField] private AudioCue backgroundMusic;
        [SerializeField] private AudioCue clickSound;
        private bool loading;
        private void Start()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            GameSettingsService.Instance.Music.Resume();
            GameSettingsService.Instance.Music.Play(backgroundMusic, 0.6f);
        }
        public void PlayClick() => GameSettingsService.Instance.Sfx.Play(clickSound);
        public void StartGame() => Load(GameSettingsService.Instance.Config.levelScenes[0]);
        public void RestartGame() => Load(SceneManager.GetActiveScene().name);
        public void MainMenu() => Load(GameSettingsService.Instance.Config.mainMenuScene);
        // Finishes the current level: on to the next one, or the credits after the last.
        public void CompleteGame() => Load(NextScene);
        public void LoadScene(string scene) => Load(scene);
        public string NextScene => GameSettingsService.Instance.Config.SceneAfter(SceneManager.GetActiveScene().name);
        private void Load(string scene)
        {
            if (loading) return;
            if (!Application.CanStreamedLevelBeLoaded(scene))
            { Debug.LogError($"Scene '{scene}' must be enabled in Build Profiles."); return; }
            loading = true;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            GameSettingsService.Instance.Save();
            GameSettingsService.Instance.Sfx.StopAll();
            SceneManager.LoadSceneAsync(scene);
        }
        public void Quit()
        {
            GameSettingsService.Instance.Save();
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
