using UnityEngine;

namespace Smoking.GameFlow
{
    [CreateAssetMenu(menuName = "Smoking/Game Settings", fileName = "GameSettings")]
    public sealed class GameSettingsConfig : ScriptableObject
    {
        [Tooltip("Lowest allowed brightness. 1 = original image; 0 = black. Applies to all scenes.")]
        [Range(0.1f, 1f)] public float minimumBrightness = 0.35f;
        [Range(0.1f, 1f)] public float defaultBrightness = 1f;
        public string mainMenuScene = "MainMenu";
        public string gameplayScene = "Gameplay";
        public string creditsScene = "Credits";
        public float MinimumBrightness => Mathf.Clamp(minimumBrightness, 0.1f, 1f);
    }
}
