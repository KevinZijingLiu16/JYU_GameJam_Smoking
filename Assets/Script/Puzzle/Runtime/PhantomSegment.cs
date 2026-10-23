using System.Collections;
using Smoking.Audio;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Smoking.Puzzle
{
    // A stair segment that starts as a translucent, non-solid phantom: no colliders, so the
    // walker finds no ground and falls. Clicking a flying book of exactly the required color
    // makes it solid in that color. Can live under a RotatableStair to combine both mechanics.
    public sealed class PhantomSegment : MonoBehaviour
    {
        // Colors closer than this count as the same book color.
        private const float ColorTolerance = 0.004f;

        [Tooltip("The exact book color that solidifies this segment. Near-miss shades are wrong answers.")]
        [SerializeField] private Color requiredColor = new Color(0.36f, 0.76f, 0.38f);
        [SerializeField] private Material phantomMaterial;
        [Tooltip("Lit material the solid segment is made from; its color is replaced by the required color.")]
        [SerializeField] private Material solidTemplate;

        [Header("Hint UI")]
        [SerializeField] private Transform hint;
        [SerializeField] private Graphic hintCover;
        [SerializeField] private Graphic hintSpine;
        [SerializeField] private float hintHeight = 1f;
        [SerializeField] private float hintBob = 0.12f;

        [SerializeField] private float popDuration = 0.35f;
        [SerializeField] private AudioCue solidifySound;

        private MeshRenderer[] renderers;
        private Collider[] colliders;
        private Material solidMaterial;
        private Camera viewCamera;
        private bool claimed;

        public Color RequiredColor => requiredColor;
        public bool IsSolid { get; private set; }
        // Not solid yet and no book on its way.
        public bool IsWaiting => !IsSolid && !claimed;
        public Vector3 HintPosition => hint != null ? hint.position : transform.position;

        public bool Accepts(Color color) =>
            Mathf.Abs(color.r - requiredColor.r) < ColorTolerance &&
            Mathf.Abs(color.g - requiredColor.g) < ColorTolerance &&
            Mathf.Abs(color.b - requiredColor.b) < ColorTolerance;

        private void Awake()
        {
            renderers = GetComponentsInChildren<MeshRenderer>();
            colliders = GetComponentsInChildren<Collider>();
            solidMaterial = new Material(solidTemplate) { name = solidTemplate.name + " (" + name + ")" };
            solidMaterial.SetColor("_BaseColor", new Color(requiredColor.r, requiredColor.g, requiredColor.b, 1f));
            viewCamera = Camera.main;
            ApplyHintColor();
            SetSolid(false);
        }

        private void OnDestroy() => Destroy(solidMaterial);

        private void OnValidate() => ApplyHintColor();

        // A matching book was clicked and is flying here: stop offering this segment to other books.
        public void Claim() => claimed = true;

        public void Solidify()
        {
            if (IsSolid) return;
            IsSolid = true;
            if (hint != null) hint.gameObject.SetActive(false);
            SetSolid(true);
            AudioBootstrap.SharedSfx?.Play(solidifySound);
            StartCoroutine(Pop());
        }

        private void LateUpdate()
        {
            if (hint == null || !hint.gameObject.activeSelf || renderers.Length == 0) return;
            Bounds bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            float bob = Mathf.Sin(Time.time * 2.2f) * hintBob;
            hint.position = new Vector3(bounds.center.x, bounds.max.y + hintHeight + bob, bounds.center.z);
            if (viewCamera != null) hint.rotation = viewCamera.transform.rotation;
        }

        private void SetSolid(bool solid)
        {
            foreach (var r in renderers)
            {
                r.sharedMaterial = solid ? solidMaterial : phantomMaterial;
                r.shadowCastingMode = solid ? ShadowCastingMode.On : ShadowCastingMode.Off;
            }
            foreach (var c in colliders) c.enabled = solid;
        }

        private void ApplyHintColor()
        {
            Color color = new Color(requiredColor.r, requiredColor.g, requiredColor.b, 1f);
            if (hintCover != null) hintCover.color = color;
            if (hintSpine != null) hintSpine.color = Color.Lerp(color, Color.black, 0.3f);
        }

        private IEnumerator Pop()
        {
            Vector3 baseScale = transform.localScale;
            for (float t = 0f; t < popDuration; t += Time.deltaTime)
            {
                transform.localScale = baseScale * (1f + 0.08f * Mathf.Sin(t / popDuration * Mathf.PI));
                yield return null;
            }
            transform.localScale = baseScale;
        }
    }
}
