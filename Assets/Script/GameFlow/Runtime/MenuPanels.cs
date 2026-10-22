using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Smoking.GameFlow
{
    /// <summary>Owns menu navigation and the gameplay pause lifecycle, not settings storage.</summary>
    public sealed class MenuPanels : MonoBehaviour
    {
        [SerializeField] private bool gameplay;
        [SerializeField] private GameObject homePanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject homeSelection;
        [SerializeField] private GameObject settingsSelection;
        [SerializeField] private GameObject pauseSelection;
        private float previousTimeScale = 1f;
        private CursorLockMode previousLock;
        private bool previousCursor;
        public bool IsPaused { get; private set; }

        private void Start() { if (!gameplay) Select(homeSelection); }
        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) HandleEscape();
        }
        public void HandleEscape()
        {
            if (settingsPanel.activeSelf) { CloseSettings(); return; }
            if (!gameplay) return;
            if (IsPaused) Resume(); else Pause();
        }
        public void OpenSettings()
        {
            if (gameplay && !IsPaused) Pause();
            if (homePanel != null) homePanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
            settingsPanel.SetActive(true);
            Select(settingsSelection);
        }
        public void CloseSettings()
        {
            settingsPanel.SetActive(false);
            if (gameplay) { pausePanel.SetActive(true); Select(pauseSelection); }
            else { homePanel.SetActive(true); Select(homeSelection); }
        }
        public void Pause()
        {
            if (!gameplay || IsPaused) return;
            previousTimeScale = Time.timeScale;
            previousLock = Cursor.lockState;
            previousCursor = Cursor.visible;
            IsPaused = true;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (homePanel != null) homePanel.SetActive(false);
            pausePanel.SetActive(true);
            Select(pauseSelection);
        }
        public void Resume()
        {
            if (!IsPaused) return;
            settingsPanel.SetActive(false);
            pausePanel.SetActive(false);
            if (homePanel != null) homePanel.SetActive(true);
            Time.timeScale = previousTimeScale;
            Cursor.lockState = previousLock;
            Cursor.visible = previousCursor;
            IsPaused = false;
            Select(null);
        }
        private static void Select(GameObject target)
        {
            if (EventSystem.current == null) return;
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(target);
        }
        private void OnDestroy()
        {
            if (IsPaused) { Time.timeScale = previousTimeScale; Cursor.lockState = previousLock; Cursor.visible = previousCursor; }
        }
    }
}
