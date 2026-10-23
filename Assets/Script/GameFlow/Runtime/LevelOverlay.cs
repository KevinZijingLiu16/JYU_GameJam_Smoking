using System.Collections;
using TMPro;
using UnityEngine;

namespace Smoking.GameFlow
{
    // Screen-space layer above the level for tutorial hints and the end-of-level fade.
    // Styled in the LevelOverlay prefab (GameFlow/Resources). A copy placed in a scene is used as is;
    // otherwise the prefab is spawned on first use. Hints never take clicks: their CanvasGroup is
    // forced non-blocking, so the puzzle under the text stays clickable whatever the styling.
    public sealed class LevelOverlay : MonoBehaviour
    {
        private const string PrefabPath = "LevelOverlay";
        private static LevelOverlay instance;

        [Header("Hint")]
        [SerializeField] private CanvasGroup hintGroup;
        [SerializeField] private TMP_Text hintText;
        [SerializeField] private float hintFadeTime = 0.35f;

        [Header("Level transition")]
        [Tooltip("The whole black screen; faded in first.")]
        [SerializeField] private CanvasGroup fadeGroup;
        [Tooltip("Title and caption; faded in once the screen is black.")]
        [SerializeField] private CanvasGroup titleGroup;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text subtitle;

        private Coroutine hintRoutine;
        private Object hintOwner;

        public static bool Exists => instance != null;

        public static LevelOverlay Instance
        {
            get
            {
                if (instance == null) instance = FindFirstObjectByType<LevelOverlay>();
                if (instance == null) instance = Instantiate(Resources.Load<LevelOverlay>(PrefabPath));
                return instance;
            }
        }

        private void Awake()
        {
            instance = this;
            Hide(hintGroup);
            Hide(fadeGroup);
            Hide(titleGroup);
        }

        // Shows `message` until the same owner hides it.
        public void ShowHint(Object owner, string message)
        {
            hintOwner = owner;
            hintText.text = message;
            FadeHint(1f);
        }

        public void HideHint(Object owner)
        {
            if (hintOwner == owner) FadeHint(0f);
        }

        // Fades to black with a title card; runs on unscaled time so a paused game cannot stall it.
        public IEnumerator PlayTransition(string heading, string caption, float fadeTime, float holdTime)
        {
            HideHint(hintOwner);
            title.text = heading;
            subtitle.text = caption;
            fadeGroup.blocksRaycasts = true; // the level is over: swallow clicks so puzzles stop reacting
            yield return Tween(fadeTime, k => fadeGroup.alpha = k);
            yield return Tween(fadeTime, k => titleGroup.alpha = k);
            yield return new WaitForSecondsRealtime(holdTime);
        }

        private void FadeHint(float target)
        {
            if (hintRoutine != null) StopCoroutine(hintRoutine);
            float from = hintGroup.alpha;
            hintRoutine = StartCoroutine(Tween(hintFadeTime, k => hintGroup.alpha = Mathf.Lerp(from, target, k)));
        }

        private static void Hide(CanvasGroup group)
        {
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        private static IEnumerator Tween(float duration, System.Action<float> apply)
        {
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                apply(Mathf.SmoothStep(0f, 1f, t / duration));
                yield return null;
            }
            apply(1f);
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
