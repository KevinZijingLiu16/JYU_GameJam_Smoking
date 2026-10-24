using UnityEngine;

namespace Smoking.GameFlow
{
    // Switches to another view while the player is inside the box: activates a CinemachineCamera
    // (give it a higher priority than the main one) and deactivates it again on the way out.
    // The CinemachineBrain blends between the two, so the camera swings round smoothly.
    public sealed class CameraZone : PlayerZone
    {
        [Tooltip("The CinemachineCamera GameObject used inside the zone; keep it inactive in the scene.")]
        [SerializeField] private GameObject zoneCamera;

        protected override Color GizmoColor => new Color(0.4f, 0.8f, 1f, 0.9f);

        protected override void Start()
        {
            base.Start();
            if (zoneCamera != null) zoneCamera.SetActive(false);
        }

        protected override void OnPlayerEnter()
        {
            if (zoneCamera != null) zoneCamera.SetActive(true);
        }

        protected override void OnPlayerExit()
        {
            if (zoneCamera != null) zoneCamera.SetActive(false);
        }
    }
}
