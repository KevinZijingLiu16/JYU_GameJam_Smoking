using System.Collections;
using UnityEngine;

namespace Smoking.Puzzle
{
    // A level piece that turns in 90° steps around axes given by its controller (the magic cube),
    // whatever way the piece itself was rotated in the editor.
    // Put the pivot (this Transform) at the piece's center so it turns in place.
    public sealed class RotatableStair : MonoBehaviour
    {
        [SerializeField] private float duration = 0.35f;

        private Quaternion targetRotation;

        public bool IsRotating { get; private set; }

        private void Awake() => targetRotation = transform.localRotation;

        // Turns around a world-space axis through the pivot; the axis does not follow earlier turns.
        // Returns false while a previous turn is still playing.
        public bool TryRotate(Vector3 worldAxis, float angle)
        {
            if (IsRotating) return false;
            Vector3 axis = transform.parent != null ? transform.parent.InverseTransformDirection(worldAxis) : worldAxis;
            Quaternion from = targetRotation;
            targetRotation = Quaternion.AngleAxis(angle, axis) * from;
            StartCoroutine(Turn(transform, from, targetRotation, duration, () => IsRotating = false));
            IsRotating = true;
            return true;
        }

        // Shared eased local-rotation tween; ends exactly on the target so 90° steps never drift.
        public static IEnumerator Turn(Transform target, Quaternion from, Quaternion to, float duration, System.Action done = null)
        {
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                target.localRotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration));
                yield return null;
            }
            target.localRotation = to;
            done?.Invoke();
        }
    }
}
