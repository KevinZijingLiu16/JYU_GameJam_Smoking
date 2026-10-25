using Smoking.Audio;
using Smoking.Player;
using UnityEngine;

namespace Smoking.GameFlow
{
    // A mid-level checkpoint. Once the player walks through the box, falling reloads the level
    // with the character starting at the path point nearest to this transform. Place it where
    // nothing ahead depends on puzzles behind it: the level is reloaded, so those are unsolved again.
    public sealed class Checkpoint : PlayerZone
    {
        [SerializeField] private AudioCue reachedSound;

        protected override Color GizmoColor => new Color(0.4f, 1f, 0.5f, 0.9f);

        protected override void OnPlayerEnter()
        {
            if (RespawnPoint.TryGet(out Vector3 current) && current == transform.position) return;
            RespawnPoint.Set(transform.position);
            GameSettingsService.Instance.Sfx.Play(reachedSound);
        }
    }
}
