using UnityEngine;

namespace Smoking.GameFlow
{
    public sealed class BrightnessOverlay : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Image shade;
        private GameSettingsService settings;
        private void Start()
        {
            settings = GameSettingsService.Instance;
            settings.BrightnessChanged += Apply;
            Apply(settings.Brightness);
        }
        private void Apply(float brightness) => shade.color = new Color(0f, 0f, 0f, 1f - brightness);
        private void OnDestroy() { if (settings != null) settings.BrightnessChanged -= Apply; }
    }
}
