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

        [Header("Quote")]
        [Tooltip("Quote and author; faded in after the title.")]
        [SerializeField] private CanvasGroup quoteGroup;
        [Tooltip("Holds the quote with the author on a last, smaller line.")]
        [SerializeField] private TMP_Text quoteText;
        [SerializeField] private Color authorColor = new Color(0.863f, 0.624f, 0.408f);
        [Tooltip("Reading pace used to decide how long a quote stays up.")]
        [SerializeField] private float charactersPerSecond = 14f;
        [SerializeField] private Vector2 quoteHoldRange = new Vector2(3.5f, 10f);
        [Tooltip("Seconds before a key press or click may skip the quote.")]
        [SerializeField] private float skipDelay = 1f;

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
            if (quoteGroup != null) Hide(quoteGroup);
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

        // Fades to black with a title card and, if given, a quote that stays up long enough to read
        // (a key press or click skips it). Runs on unscaled time so a paused game cannot stall it.
        public IEnumerator PlayTransition(string heading, string caption, string quote, string author, float fadeTime, float holdTime)
        {
            HideHint(hintOwner);
            title.text = heading;
            subtitle.text = caption;
            bool hasQuote = !string.IsNullOrEmpty(quote) && quoteGroup != null;
            if (hasQuote)
            {
                quoteText.text = string.IsNullOrEmpty(author) ? quote
                    : $"{quote}\n<size=72%><color=#{ColorUtility.ToHtmlStringRGB(authorColor)}>— {author}</color></size>";
            }
            fadeGroup.blocksRaycasts = true; // the level is over: swallow clicks so puzzles stop reacting
            yield return Tween(fadeTime, k => fadeGroup.alpha = k);
            yield return Tween(fadeTime, k => titleGroup.alpha = k);
            if (!hasQuote)
            {
                yield return new WaitForSecondsRealtime(holdTime);
                yield break;
            }
            yield return Tween(fadeTime * 1.5f, k => quoteGroup.alpha = k);
            float hold = Mathf.Clamp(quote.Length / Mathf.Max(1f, charactersPerSecond), quoteHoldRange.x, quoteHoldRange.y);
            for (float t = 0f; t < hold; t += Time.unscaledDeltaTime)
            {
                if (t > skipDelay && SkipPressed()) break;
                yield return null;
            }
        }

        private static bool SkipPressed()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            var pointer = UnityEngine.InputSystem.Pointer.current;
            return (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
                || (pointer != null && pointer.press.wasPressedThisFrame);
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
