using TMPro;
using UnityEngine;

namespace Smoking.GameFlow
{
    public sealed class CreditsScroller : MonoBehaviour
    {
        [SerializeField] private RectTransform viewport;
        [SerializeField] private TMP_Text creditsText;
        [SerializeField, Min(1f)] private float pixelsPerSecond = 65f;
        [SerializeField, Min(0f)] private float startDelay = 1f;
        [SerializeField] private bool loop;
        private float elapsed;
        private float endPosition;
        private bool finished;
        public bool Finished => finished;

        private void Start() => RestartRoll();
        public void RestartRoll()
        {
            Canvas.ForceUpdateCanvases();
            creditsText.ForceMeshUpdate();
            float height = creditsText.preferredHeight + 60f;
            creditsText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            float edge = (viewport.rect.height + height) * 0.5f;
            creditsText.rectTransform.anchoredPosition = new Vector2(0f, -edge);
            endPosition = edge;
            elapsed = 0f;
            finished = false;
        }
        private void Update()
        {
            if (finished) return;
            elapsed += Time.unscaledDeltaTime;
            if (elapsed < startDelay) return;
            var rect = creditsText.rectTransform;
            rect.anchoredPosition += Vector2.up * (pixelsPerSecond * Time.unscaledDeltaTime);
            if (rect.anchoredPosition.y >= endPosition)
            { if (loop) RestartRoll(); else finished = true; }
        }
    }
}
