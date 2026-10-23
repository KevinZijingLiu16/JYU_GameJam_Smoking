using Smoking.Audio;
using UnityEngine;

namespace Smoking.Puzzle
{
    // The magic cube under a staircase. Each face carries a trigger (TriggerX / TriggerY / TriggerZ);
    // clicking a trigger, or the cube face it sits on, turns the staircase -90° around that local
    // axis and spins the trigger itself -90° around its own Z.
    public sealed class MagicCube : MonoBehaviour, IClickable
    {
        [SerializeField] private RotatableStair target;
        [SerializeField] private Transform triggerX;
        [SerializeField] private Transform triggerY;
        [SerializeField] private Transform triggerZ;
        [SerializeField] private Collider cube;
        [SerializeField] private float angle = -90f;
        [SerializeField] private float triggerSpinDuration = 0.35f;
        [SerializeField] private AudioCue rotateSound;

        public void OnClicked(RaycastHit hit)
        {
            Transform trigger = FindTrigger(hit);
            if (trigger == null || target == null) return;

            Vector3 axis = trigger == triggerX ? Vector3.right : trigger == triggerY ? Vector3.up : Vector3.forward;
            if (!target.TryRotate(axis, angle)) return;
            AudioBootstrap.SharedSfx?.Play(rotateSound);

            Quaternion from = trigger.localRotation;
            StartCoroutine(RotatableStair.Turn(trigger, from, from * Quaternion.AngleAxis(angle, Vector3.forward), triggerSpinDuration));
        }

        private Transform FindTrigger(RaycastHit hit)
        {
            foreach (var trigger in Triggers())
                if (hit.transform.IsChildOf(trigger)) return trigger;

            // Clicked the bare cube: pick the trigger sitting on the face that was hit.
            if (hit.collider != cube) return null;
            Transform best = null;
            float bestDot = 0.5f;
            foreach (var trigger in Triggers())
            {
                float dot = Vector3.Dot(hit.normal, (FaceCenter(trigger) - cube.bounds.center).normalized);
                if (dot > bestDot) { bestDot = dot; best = trigger; }
            }
            return best;
        }

        private Transform[] Triggers() => new[] { triggerX, triggerY, triggerZ };

        private static Vector3 FaceCenter(Transform trigger)
        {
            var renderers = trigger.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return trigger.position;
            Bounds bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            return bounds.center;
        }
    }
}
