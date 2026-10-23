using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Smoking.GameFlow
{
    // Screen-space layer above the level for tutorial hints and the end-of-level fade.
    // Built on first use, so scenes need no extra UI setup.
    public sealed class LevelOverlay : MonoBehaviour
    {
        private static readonly Color Paper = new Color32(241, 233, 215, 255);
        private static readonly Color Accent = new Color32(220, 159, 104, 255);
        private static LevelOverlay instance;

        private CanvasGroup hintGroup;
        private TextMeshProUGUI hintText;
        private Image fade;
        private TextMeshProUGUI title;
        private TextMeshProUGUI subtitle;
        private Coroutine hintRoutine;
        private string hintOwner;

        public static LevelOverlay Instance
        {
            get
            {
                if (instance == null) instance = Build();
                return instance;
            }
        }

        // Shows `message` near the bottom of the screen until the same owner hides it.
        public void ShowHint(string owner, string message)
        {
            hintOwner = owner;
            hintText.text = message;
            FadeHint(1f);
        }

        public void HideHint(string owner)
        {
            if (hintOwner == owner) FadeHint(0f);
        }

        // Fades to black with a title card; runs on unscaled time so a paused game cannot stall it.
        public IEnumerator PlayTransition(string heading, string caption, float fadeTime, float holdTime)
        {
            HideHint(hintOwner);
            title.text = heading;
            subtitle.text = caption;
            fade.raycastTarget = true; // swallow clicks so puzzles stop reacting
            yield return Tween(fadeTime, k => fade.color = new Color(0f, 0f, 0f, k));
            yield return Tween(fadeTime, k => title.alpha = subtitle.alpha = k);
            yield return new WaitForSecondsRealtime(holdTime);
        }

        private void FadeHint(float target)
        {
            if (hintRoutine != null) StopCoroutine(hintRoutine);
            float from = hintGroup.alpha;
            hintRoutine = StartCoroutine(Tween(0.35f, k => hintGroup.alpha = Mathf.Lerp(from, target, k)));
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

        private static LevelOverlay Build()
        {
            var go = new GameObject("LevelOverlay", typeof(Canvas), typeof(CanvasScaler));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500; // above the HUD, below nothing
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            var overlay = go.AddComponent<LevelOverlay>();

            var hint = new GameObject("Hint", typeof(RectTransform), typeof(CanvasGroup));
            hint.transform.SetParent(go.transform, false);
            Area((RectTransform)hint.transform, 0.15f, 0.06f, 0.85f, 0.2f);
            overlay.hintGroup = hint.GetComponent<CanvasGroup>();
            overlay.hintGroup.alpha = 0f;
            overlay.hintGroup.blocksRaycasts = false;
            var plate = Panel("Plate", hint.transform, new Color(0.04f, 0.06f, 0.06f, 0.78f));
            Area(plate.rectTransform, 0f, 0f, 1f, 1f);
            overlay.hintText = Label("Text", hint.transform, 34f, Paper);
            Area(overlay.hintText.rectTransform, 0.04f, 0.08f, 0.96f, 0.92f);

            overlay.fade = Panel("Fade", go.transform, new Color(0f, 0f, 0f, 0f));
            Area(overlay.fade.rectTransform, 0f, 0f, 1f, 1f);
            overlay.title = Label("Title", overlay.fade.transform, 96f, Paper);
            overlay.title.fontStyle = FontStyles.Bold;
            overlay.title.alpha = 0f;
            Area(overlay.title.rectTransform, 0.1f, 0.48f, 0.9f, 0.66f);
            overlay.subtitle = Label("Subtitle", overlay.fade.transform, 30f, Accent);
            overlay.subtitle.alpha = 0f;
            Area(overlay.subtitle.rectTransform, 0.1f, 0.38f, 0.9f, 0.47f);
            return overlay;
        }

        private static Image Panel(string name, Transform parent, Color color)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI Label(string name, Transform parent, float size, Color color)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(parent, false);
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        private static void Area(RectTransform rect, float xMin, float yMin, float xMax, float yMax)
        {
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
