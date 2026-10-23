using System.Collections;
using UnityEngine;

namespace Smoking.Puzzle
{
    // A level piece that turns around its own local X / Y / Z axis in 90° steps.
    // Put the pivot (this Transform) at the piece's center so it turns in place.
    public sealed class RotatableStair : MonoBehaviour
    {
        [SerializeField] private float duration = 0.35f;

        private Quaternion targetRotation;

        public bool IsRotating { get; private set; }

        private void Awake() => targetRotation = transform.localRotation;

        // Returns false while a previous turn is still playing.
        public bool TryRotate(Vector3 localAxis, float angle)
        {
            if (IsRotating) return false;
            Quaternion from = targetRotation;
            targetRotation = from * Quaternion.AngleAxis(angle, localAxis);
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
