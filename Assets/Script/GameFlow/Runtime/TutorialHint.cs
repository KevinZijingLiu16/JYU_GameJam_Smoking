using UnityEngine;

namespace Smoking.GameFlow
{
    // Shows a tutorial line at the bottom of the screen while the player is inside the box.
    public sealed class TutorialHint : PlayerZone
    {
        [TextArea(2, 4)]
        [SerializeField] private string message = "Click the cube to turn the stairs.";
        [Tooltip("Show only the first time the player walks in.")]
        [SerializeField] private bool once;

        private bool shown;

        protected override Color GizmoColor => new Color(0.4f, 0.85f, 0.75f, 0.9f);

        protected override void OnPlayerEnter()
        {
            if (once && shown) return;
            shown = true;
            LevelOverlay.Instance.ShowHint(this, message);
        }

        protected override void OnPlayerExit() => LevelOverlay.Instance.HideHint(this);

        private void OnDisable()
        {
            if (shown && LevelOverlay.Exists) LevelOverlay.Instance.HideHint(this);
        }
    }
}
