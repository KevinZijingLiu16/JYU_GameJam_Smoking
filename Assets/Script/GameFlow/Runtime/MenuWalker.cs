using UnityEngine;
using UnityEngine.UI;

namespace Smoking.GameFlow
{
    // A character pacing back and forth across the main menu. Its route is drawn on the canvas, as
    // RectTransform children of `route` marking where the feet go. A dedicated orthographic camera
    // films the character into a RawImage placed behind the stair art, so the handrail hides its legs.
    public sealed class MenuWalker : MonoBehaviour
    {
        [Tooltip("Children are the feet positions, in any order: a polyline of floors and step risers.")]
        [SerializeField] private RectTransform route;
        [Tooltip("Full-screen RawImage the character is shown in. Its place among the UI siblings decides what covers it.")]
        [SerializeField] private RawImage view;
        [SerializeField] private Camera filmCamera;
        [Tooltip("Scaled so that it stands `height` canvas units tall.")]
        [SerializeField] private Transform character;
        [SerializeField] private Animator animator;

        [Header("Size and pace")]
        [Tooltip("Character height in canvas units (the canvas is 1080 tall).")]
        [SerializeField] private float height = 340f;
        [Tooltip("Real height of the character model at scale 1, in meters.")]
        [SerializeField] private float modelHeight = 1.7f;
        [Tooltip("Walking speed in meters per second, at the character's own scale.")]
        [SerializeField] private float walkSpeed = 1.1f;
        [Tooltip("Speed at which the walk clip plays at 1x (m/s).")]
        [SerializeField] private float animationReferenceSpeed = 1.4f;
        [Tooltip("How fast the body rises or drops onto the next step, in canvas units per second.")]
        [SerializeField] private float stepSpeed = 320f;
        [SerializeField] private float turnSpeed = 360f;
        [Tooltip("How much the character turns toward the viewer while walking, 0 = pure profile.")]
        [SerializeField, Range(0f, 1f)] private float faceViewer = 0.35f;

        // 100 canvas units = 1 world unit in front of the film camera.
        private const float UnitsPerMeter = 100f;

        private RectTransform canvasRect;
        private RenderTexture texture;
        private float minX, maxX;
        private Vector2 feet;
        private int direction = -1;

        private void Start()
        {
            canvasRect = (RectTransform)view.canvas.rootCanvas.transform;
            if (animator == null) animator = character.GetComponentInChildren<Animator>();
            if (animator != null) animator.applyRootMotion = false;
            view.raycastTarget = false;

            minX = float.MaxValue;
            maxX = float.MinValue;
            foreach (RectTransform point in route)
            {
                float x = ToCanvas(point).x;
                minX = Mathf.Min(minX, x);
                maxX = Mathf.Max(maxX, x);
            }
            feet = new Vector2(maxX, Ground(maxX));
            character.rotation = Facing();
        }

        private void OnDestroy()
        {
            if (texture != null) texture.Release();
        }

        private void LateUpdate()
        {
            FitCamera();
            Walk(Time.deltaTime);

            character.localScale = Vector3.one * (height / UnitsPerMeter / modelHeight);
            character.position = filmCamera.transform.position + new Vector3(feet.x, feet.y, 0f) / UnitsPerMeter
                                 + filmCamera.transform.forward * 10f;
            if (animator != null) animator.speed = walkSpeed / animationReferenceSpeed;
        }

        private void Walk(float dt)
        {
            Quaternion facing = Facing();
            if (Quaternion.Angle(character.rotation, facing) > 1f)
            {
                // Turning around at an end: on the spot.
                character.rotation = Quaternion.RotateTowards(character.rotation, facing, turnSpeed * dt);
                return;
            }

            feet.x += direction * walkSpeed * (height / modelHeight) * dt;
            if (feet.x <= minX || feet.x >= maxX)
            {
                feet.x = Mathf.Clamp(feet.x, minX, maxX);
                direction = -direction;
            }
            feet.y = Mathf.MoveTowards(feet.y, Ground(feet.x), stepSpeed * dt);
        }

        private Quaternion Facing() => Quaternion.LookRotation(new Vector3(direction, 0f, -faceViewer));

        // Highest stretch of the route above x; risers (vertical stretches) are skipped.
        private float Ground(float x)
        {
            float best = float.MinValue;
            int count = route.childCount;
            var points = new Vector2[count];
            for (int i = 0; i < count; i++) points[i] = ToCanvas((RectTransform)route.GetChild(i));
            for (int i = 0; i + 1 < count; i++)
            {
                Vector2 a = points[i], b = points[i + 1];
                if (Mathf.Approximately(a.x, b.x) || x < Mathf.Min(a.x, b.x) || x > Mathf.Max(a.x, b.x)) continue;
                best = Mathf.Max(best, Mathf.Lerp(a.y, b.y, (x - a.x) / (b.x - a.x)));
            }
            return best > float.MinValue ? best : feet.y;
        }

        private Vector2 ToCanvas(RectTransform point) => canvasRect.InverseTransformPoint(point.position);

        // The camera sees exactly the canvas, at the screen's resolution, with a transparent background.
        private void FitCamera()
        {
            if (texture == null || texture.width != Screen.width || texture.height != Screen.height)
            {
                if (texture != null) texture.Release();
                texture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32) { name = "MenuWalker" };
                filmCamera.targetTexture = texture;
                view.texture = texture;
            }
            Rect rect = canvasRect.rect;
            filmCamera.orthographic = true;
            filmCamera.orthographicSize = rect.height / 2f / UnitsPerMeter;
            filmCamera.aspect = rect.width / rect.height;
            filmCamera.clearFlags = CameraClearFlags.SolidColor;
            filmCamera.backgroundColor = Color.clear;
            filmCamera.allowHDR = false;
        }

        private void OnDrawGizmos()
        {
            if (route == null) return;
            Gizmos.color = new Color(1f, 0.75f, 0.2f);
            for (int i = 0; i + 1 < route.childCount; i++)
                Gizmos.DrawLine(route.GetChild(i).position, route.GetChild(i + 1).position);
        }
    }
}
