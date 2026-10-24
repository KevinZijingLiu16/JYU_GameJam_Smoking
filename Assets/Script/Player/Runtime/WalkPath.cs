using System.Collections.Generic;
using UnityEngine;

namespace Smoking.Player
{
    // A designer-authored path: every child Transform is a waypoint, in hierarchy order.
    // Move / add / reorder the children to reshape the path.
    public sealed class WalkPath : MonoBehaviour
    {
        [SerializeField] private Color gizmoColor = new Color(1f, 0.75f, 0.2f);

        private readonly List<Vector3> points = new List<Vector3>();
        private readonly List<float> distances = new List<float>();

        public float Length { get; private set; }
        public int PointCount => points.Count;

        private void Awake() => Rebuild();

        // Call again if waypoints are moved at runtime (e.g. rotating Monument Valley pieces).
        public void Rebuild()
        {
            points.Clear();
            distances.Clear();
            Length = 0f;
            foreach (Transform child in transform)
            {
                if (points.Count > 0) Length += Vector3.Distance(points[points.Count - 1], child.position);
                points.Add(child.position);
                distances.Add(Length);
            }
        }

        // Point and travel direction at a distance along the path.
        public Vector3 Evaluate(float distance, out Vector3 forward)
        {
            forward = Vector3.forward;
            if (points.Count == 0) return transform.position;
            if (points.Count == 1) return points[0];

            distance = Mathf.Clamp(distance, 0f, Length);
            int i = 1;
            while (i < points.Count - 1 && distances[i] < distance) i++;

            Vector3 a = points[i - 1], b = points[i];
            float segment = distances[i] - distances[i - 1];
            float t = segment > 0f ? (distance - distances[i - 1]) / segment : 0f;
            forward = (b - a).normalized;
            return Vector3.Lerp(a, b, t);
        }

        // Distance along the path of the path point nearest to `point`.
        public float ClosestDistance(Vector3 point)
        {
            float best = 0f, bestSqr = float.MaxValue;
            for (int i = 1; i < points.Count; i++)
            {
                Vector3 a = points[i - 1], ab = points[i] - a;
                float t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector3.Dot(point - a, ab) / ab.sqrMagnitude) : 0f;
                float sqr = (a + ab * t - point).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; best = distances[i - 1] + ab.magnitude * t; }
            }
            return best;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Transform previous = null;
            foreach (Transform child in transform)
            {
                Gizmos.DrawSphere(child.position, 0.12f);
                if (previous != null) Gizmos.DrawLine(previous.position, child.position);
                previous = child;
            }
        }
    }
}
