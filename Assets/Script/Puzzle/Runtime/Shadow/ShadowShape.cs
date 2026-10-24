using System.Collections.Generic;
using UnityEngine;

namespace Smoking.Puzzle
{
    // An object whose shadow on the ShadowWall the shadow walker can stand on. The shadow outline
    // is the convex hull of the projected mesh vertices, so use boxes and other convex shapes.
    [RequireComponent(typeof(MeshFilter))]
    public sealed class ShadowShape : MonoBehaviour
    {
        [Tooltip("The shadow is ground and wall for the shadow walker. Off: decoration only (e.g. a lever).")]
        [SerializeField] private bool solid = true;
        [Tooltip("Standing on this shadow solves the puzzle.")]
        [SerializeField] private bool goal;

        private readonly List<Vector2> projected = new List<Vector2>();
        private Vector3[] vertices;

        public bool Solid => solid;
        public bool Goal => goal;
        // Shadow outline in wall coordinates, counter-clockwise; empty when nothing is cast.
        public List<Vector2> Outline { get; } = new List<Vector2>();

        private void Awake() => vertices = new List<Vector3>(new HashSet<Vector3>(GetComponent<MeshFilter>().sharedMesh.vertices)).ToArray();

        public void Rebuild(ShadowWall wall, Vector3 light)
        {
            Outline.Clear();
            projected.Clear();
            foreach (var v in vertices)
            {
                if (!wall.TryProject(light, transform.TransformPoint(v), out Vector2 p)) return;
                projected.Add(p);
            }
            ConvexHull(projected, Outline);
        }

        // Bottom and top of the shadow along the vertical line at x.
        public bool SpanAt(float x, out float bottom, out float top)
        {
            bottom = float.MaxValue;
            top = float.MinValue;
            for (int i = 0; i < Outline.Count; i++)
            {
                Vector2 a = Outline[i], b = Outline[(i + 1) % Outline.Count];
                if (x < Mathf.Min(a.x, b.x) || x > Mathf.Max(a.x, b.x)) continue;
                float y0 = a.y, y1 = b.y;
                if (!Mathf.Approximately(a.x, b.x)) y0 = y1 = Mathf.Lerp(a.y, b.y, (x - a.x) / (b.x - a.x));
                bottom = Mathf.Min(bottom, Mathf.Min(y0, y1));
                top = Mathf.Max(top, Mathf.Max(y0, y1));
            }
            return top >= bottom;
        }

        // Andrew's monotone chain.
        private static void ConvexHull(List<Vector2> points, List<Vector2> hull)
        {
            if (points.Count < 3) return;
            points.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            for (int pass = 0; pass < 2; pass++)
            {
                int start = hull.Count;
                for (int i = 0; i < points.Count; i++)
                {
                    Vector2 p = points[pass == 0 ? i : points.Count - 1 - i];
                    while (hull.Count >= start + 2 && Cross(hull[hull.Count - 2], hull[hull.Count - 1], p) <= 0f)
                        hull.RemoveAt(hull.Count - 1);
                    hull.Add(p);
                }
                hull.RemoveAt(hull.Count - 1);
            }
        }

        private static float Cross(Vector2 o, Vector2 a, Vector2 b) => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
    }
}
