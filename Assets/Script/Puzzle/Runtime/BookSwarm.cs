using System;
using System.Collections.Generic;
using UnityEngine;

namespace Smoking.Puzzle
{
    // A book area. While the player is inside the area box, books fly all over the screen:
    // copies of each linked segment's required color plus the designer's decoy colors.
    // Leaving unsolved sends the books away; coming back brings them again. Once every linked
    // segment is solid, the books leave for good.
    [DefaultExecutionOrder(10000)] // after the camera has moved this frame
    public sealed class BookSwarm : MonoBehaviour
    {
        [Serializable]
        public struct DecoyBooks
        {
            public Color color;
            [Min(0)] public int count;
        }

        [Header("Area")]
        [Tooltip("Box around this transform (local space). Books fly while the player stands inside it.")]
        [SerializeField] private Vector3 areaSize = new Vector3(6f, 4f, 4f);
        [SerializeField] private Transform player;

        [Header("Puzzle")]
        [Tooltip("The phantom segments this area's books can solidify.")]
        [SerializeField] private PhantomSegment[] segments;
        [Tooltip("Books of the right color spawned per segment.")]
        [SerializeField, Min(1)] private int answerCopies = 2;
        [Tooltip("Wrong books. Use shades close to the answers to make the puzzle harder.")]
        [SerializeField] private List<DecoyBooks> decoys = new List<DecoyBooks>();

        [Header("Books")]
        [Tooltip("Plain book meshes; a Book component and click collider are added at spawn.")]
        [SerializeField] private GameObject[] bookPrefabs;
        [SerializeField] private Material bookMaterial;
        [SerializeField] private float bookScale = 3.5f;
        [SerializeField] private float minSpeed = 1.5f;
        [SerializeField] private float maxSpeed = 3.5f;
        [Tooltip("Click collider size relative to the book mesh: bigger is more forgiving.")]
        [SerializeField] private float clickPadding = 1.4f;

        [Header("Flight volume (follows the camera)")]
        [SerializeField] private Camera viewCamera;
        [Tooltip("Distance in front of the camera of the volume's center.")]
        [SerializeField] private float distance = 8f;
        [SerializeField] private float depth = 4f;
        [Tooltip("Fraction of the screen the books roam over.")]
        [SerializeField, Range(0.3f, 1f)] private float screenFill = 0.85f;

        private readonly List<Book> books = new List<Book>();
        private Transform flock;
        private bool active;

        // Half-size of the flight volume, in the flock's local space.
        public Vector3 Extents { get; private set; }

        private bool Solved
        {
            get
            {
                foreach (var segment in segments) if (segment != null && !segment.IsSolid) return false;
                return true;
            }
        }

        private void Start()
        {
            if (viewCamera == null) viewCamera = Camera.main;
            if (player == null)
            {
                var found = GameObject.Find("Player");
                if (found != null) player = found.transform;
            }
            flock = new GameObject(name + " Books").transform;
        }

        private void LateUpdate()
        {
            FollowCamera();
            bool inside = player != null && PlayerInside();
            if (inside && !active && !Solved) Release();
            else if (!inside && active) Dismiss();
        }

        private void OnDestroy()
        {
            if (flock != null) Destroy(flock.gameObject);
        }

        public void HandleClick(Book book)
        {
            PhantomSegment segment = null;
            foreach (var s in segments)
                if (s != null && s.IsWaiting && s.Accepts(book.Tint)) { segment = s; break; }

            if (segment == null)
            {
                book.Reject();
                return;
            }

            segment.Claim();
            books.Remove(book);
            book.FlyTo(() => segment.HintPosition, () =>
            {
                segment.Solidify();
                if (Solved) Dismiss();
            });
        }

        private bool PlayerInside()
        {
            Vector3 local = transform.InverseTransformPoint(player.position);
            Vector3 half = areaSize * 0.5f;
            return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z;
        }

        private void Release()
        {
            active = true;
            foreach (var segment in segments)
                if (segment != null && segment.IsWaiting)
                    for (int i = 0; i < answerCopies; i++) Spawn(segment.RequiredColor);
            foreach (var decoy in decoys)
                for (int i = 0; i < decoy.count; i++) Spawn(decoy.color);
        }

        private void Dismiss()
        {
            active = false;
            foreach (var book in books) book.Leave();
            books.Clear();
        }

        private void FollowCamera()
        {
            Transform cam = viewCamera.transform;
            flock.SetPositionAndRotation(cam.position + cam.forward * distance, cam.rotation);

            float halfHeight = viewCamera.orthographic
                ? viewCamera.orthographicSize
                : distance * Mathf.Tan(viewCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            Extents = new Vector3(halfHeight * viewCamera.aspect, halfHeight, depth * 0.5f) * screenFill;
        }

        private void Spawn(Color color)
        {
            var go = Instantiate(bookPrefabs[UnityEngine.Random.Range(0, bookPrefabs.Length)], flock);
            go.name = "Book_" + ColorUtility.ToHtmlStringRGB(color);
            go.transform.localPosition = Vector3.Scale(
                new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)), Extents);
            if (bookMaterial != null) go.GetComponent<Renderer>().sharedMaterial = bookMaterial;

            var mesh = go.GetComponent<MeshFilter>().sharedMesh;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true; // the walker's ground probe ignores triggers
            box.center = mesh.bounds.center;
            box.size = mesh.bounds.size * clickPadding;

            var book = go.AddComponent<Book>();
            book.Init(this, color, bookScale, minSpeed, maxSpeed);
            books.Add(book);
        }

        private void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.55f, 0.35f, 0.95f, 0.08f);
            Gizmos.DrawCube(Vector3.zero, areaSize);
            Gizmos.color = new Color(0.55f, 0.35f, 0.95f, 0.9f);
            Gizmos.DrawWireCube(Vector3.zero, areaSize);
        }

        private void OnDrawGizmosSelected()
        {
            if (segments == null) return;
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = new Color(0.55f, 0.35f, 0.95f, 0.9f);
            foreach (var segment in segments)
                if (segment != null) Gizmos.DrawLine(transform.position, segment.transform.position);
        }
    }
}
