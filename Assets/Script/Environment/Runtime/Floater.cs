using UnityEngine;

namespace Smoking.Environment
{
    // Background decoration: drifts up and down around its start position and slowly turns,
    // like an enchanted book hanging in the air. Each copy starts at a random phase.
    public sealed class Floater : MonoBehaviour
    {
        [SerializeField] private float bobHeight = 0.25f;
        [SerializeField] private float bobPeriod = 4f;
        [Tooltip("Degrees per second around the world Y axis.")]
        [SerializeField] private float spinSpeed = 12f;
        [Tooltip("Gentle rocking around the local X axis, in degrees.")]
        [SerializeField] private float tilt = 8f;

        private Vector3 start;
        private Quaternion baseRotation;
        private float phase;

        private void Start()
        {
            start = transform.localPosition;
            baseRotation = transform.localRotation;
            phase = Random.value * Mathf.PI * 2f;
            if (Random.value < 0.5f) spinSpeed = -spinSpeed;
        }

        private void Update()
        {
            float t = Time.time * Mathf.PI * 2f / bobPeriod + phase;
            transform.localPosition = start + Vector3.up * (Mathf.Sin(t) * bobHeight);
            transform.localRotation = Quaternion.Euler(0f, Time.time * spinSpeed + phase * Mathf.Rad2Deg, 0f)
                                      * baseRotation * Quaternion.Euler(Mathf.Sin(t * 0.7f) * tilt, 0f, 0f);
        }
    }
}
