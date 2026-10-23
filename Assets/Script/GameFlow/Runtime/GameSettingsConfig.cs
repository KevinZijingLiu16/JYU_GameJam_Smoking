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
        [Tooltip("Levels in play order. START GAME opens the first; finishing the last one rolls the credits.")]
        public string[] levelScenes = { "Level1_Tutorial", "Gameplay_KevinPrototype" };
        public string creditsScene = "Credits";
        public float MinimumBrightness => Mathf.Clamp(minimumBrightness, 0.1f, 1f);

        // The scene after `current` in play order: the next level, or the credits after the last one.
        public string SceneAfter(string current)
        {
            int index = System.Array.IndexOf(levelScenes, current);
            return index >= 0 && index + 1 < levelScenes.Length ? levelScenes[index + 1] : creditsScene;
        }
    }
}
