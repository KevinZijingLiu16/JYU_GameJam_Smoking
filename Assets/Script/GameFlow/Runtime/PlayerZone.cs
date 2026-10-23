using UnityEngine;

namespace Smoking.GameFlow
{
    // A box around this transform that notices when the player walks in or out. The walker moves
    // by transform, not physics, so the box is checked by position rather than with trigger events.
    public abstract class PlayerZone : MonoBehaviour
    {
        [Tooltip("Box around this transform (local space).")]
        [SerializeField] private Vector3 areaSize = new Vector3(2f, 3f, 2f);
        [Tooltip("Left empty: the scene object named \"Player\".")]
        [SerializeField] protected Transform player;

        private bool inside;

        protected abstract Color GizmoColor { get; }
        protected abstract void OnPlayerEnter();
        protected virtual void OnPlayerExit() { }

        protected virtual void Start()
        {
            if (player != null) return;
            var found = GameObject.Find("Player");
            if (found != null) player = found.transform;
        }

        private void Update()
        {
            bool now = player != null && Contains(player.position);
            if (now == inside) return;
            inside = now;
            if (inside) OnPlayerEnter(); else OnPlayerExit();
        }

        private bool Contains(Vector3 point)
        {
            Vector3 local = transform.InverseTransformPoint(point);
            Vector3 half = areaSize * 0.5f;
            return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z;
        }

        private void OnDrawGizmos()
        {
            Color color = GizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(color.r, color.g, color.b, 0.08f);
            Gizmos.DrawCube(Vector3.zero, areaSize);
            Gizmos.color = color;
            Gizmos.DrawWireCube(Vector3.zero, areaSize);
        }
    }
}
