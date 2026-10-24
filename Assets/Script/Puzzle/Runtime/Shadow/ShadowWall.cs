using UnityEngine;

namespace Smoking.Puzzle
{
    // The flat surface shadows are cast onto, and the 2D world the shadow walker lives in.
    // 2D coordinates: x runs along the wall (screen-right for a camera facing it), y runs up.
    public sealed class ShadowWall : MonoBehaviour
    {
        [Tooltip("Local axis pointing out of the wall toward the light. A Unity Plane uses its local Y.")]
        [SerializeField] private Vector3 localNormal = Vector3.up;

        public Vector3 Origin => transform.position;
        public Vector3 Normal => transform.TransformDirection(localNormal).normalized;
        public Vector3 Up => Vector3.ProjectOnPlane(Vector3.up, Normal).normalized;
        public Vector3 Right => Vector3.Cross(Up, -Normal);

        public Vector2 ToWall(Vector3 world)
        {
            Vector3 d = world - Origin;
            return new Vector2(Vector3.Dot(d, Right), Vector3.Dot(d, Up));
        }

        public Vector3 ToWorld(Vector2 point) => Origin + Right * point.x + Up * point.y;

        // Where `point`'s shadow lands. False when the point is not between the light and the wall.
        public bool TryProject(Vector3 light, Vector3 point, out Vector2 shadow)
        {
            shadow = default;
            float toWall = Vector3.Dot(Origin - light, Normal);
            float toPoint = Vector3.Dot(point - light, Normal);
            if (toWall >= 0f || toPoint >= -0.01f || toPoint <= toWall) return false;
            shadow = ToWall(light + (point - light) * (toWall / toPoint));
            return true;
        }

        // How many times larger than the object a shadow cast from `point`'s depth comes out.
        public float Magnification(Vector3 light, Vector3 point) =>
            Vector3.Dot(Origin - light, Normal) / Vector3.Dot(point - light, Normal);
    }
}
