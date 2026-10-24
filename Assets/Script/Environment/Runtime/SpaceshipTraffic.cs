using UnityEngine;

namespace Smoking.Environment
{
    // Background traffic: ships cross the camera's view on gentle curves, banking into turns, each with an
    // engine flame and a glowing trail. Routes are picked around wherever the camera is, so ships keep
    // passing by as it follows the player. Ships leave while `dimWith` is dimmed (e.g. a shadow puzzle).
    public sealed class SpaceshipTraffic : MonoBehaviour
    {
        [SerializeField] private GameObject[] shipPrefabs;
        [SerializeField] private Material[] paints;
        [SerializeField, Min(1)] private int count = 6;
        [SerializeField] private Vector2 scaleRange = new Vector2(0.18f, 0.32f);
        [Tooltip("Flying speed in world units per second.")]
        [SerializeField] private Vector2 speedRange = new Vector2(6f, 13f);
        [Tooltip("Distance in front of the camera the routes run at; keep it behind the level.")]
        [SerializeField] private Vector2 depthRange = new Vector2(30f, 75f);
        [Tooltip("Seconds a ship waits off screen before its next pass.")]
        [SerializeField] private Vector2 pauseRange = new Vector2(0.5f, 5f);

        [Header("Engine")]
        [SerializeField] private Material trailMaterial;
        [SerializeField] private Material flameMaterial;
        [SerializeField] private Color trailColor = new Color(0.45f, 0.85f, 1f);
        [SerializeField] private Color flameColor = new Color(1f, 0.6f, 0.25f);

        [Tooltip("Ships leave the sky while this light is below half its starting intensity.")]
        [SerializeField] private Light dimWith;

        private sealed class Ship
        {
            public Transform transform;
            public TrailRenderer trail;
            public ParticleSystem flame;
            public Vector3 from, control, to;
            public float t, duration, wait, roll;
            public bool flying;
        }

        private Ship[] ships;
        private Camera viewCamera;
        private float dimBase;

        private void Start()
        {
            viewCamera = Camera.main;
            dimBase = dimWith != null ? dimWith.intensity : 0f;
            ships = new Ship[count];
            for (int i = 0; i < count; i++)
            {
                ships[i] = Build(shipPrefabs[i % shipPrefabs.Length]);
                ships[i].wait = Random.Range(0f, pauseRange.y); // stagger the first passes
            }
        }

        private Ship Build(GameObject prefab)
        {
            var go = Instantiate(prefab, transform);
            foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c);
            foreach (var r in go.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // The engine sits at the back of the hull (ships face +Z).
            Bounds local = go.GetComponentInChildren<MeshFilter>().sharedMesh.bounds;
            var engine = new GameObject("Engine").transform;
            engine.SetParent(go.transform, false);
            engine.localPosition = new Vector3(local.center.x, local.center.y, local.min.z);
            engine.localRotation = Quaternion.Euler(0f, 180f, 0f); // emit backwards

            var trail = engine.gameObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = trailMaterial;
            trail.time = 0.9f;
            trail.minVertexDistance = 0.2f;
            trail.widthMultiplier = 1f;
            trail.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(trailColor, 0.3f), new GradientColorKey(trailColor, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.7f, 0.3f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = g;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;

            var flame = engine.gameObject.AddComponent<ParticleSystem>();
            flame.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = flame.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 7f);
            main.startColor = Color.white;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 60;
            var emission = flame.emission;
            emission.rateOverTime = 60f;
            var shape = flame.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 6f;
            shape.radius = 0.4f;
            var colour = flame.colorOverLifetime;
            colour.enabled = true;
            var fg = new Gradient();
            fg.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(flameColor, 0.35f), new GradientColorKey(new Color(1f, 0.2f, 0.1f), 1f) },
                       new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) });
            colour.color = fg;
            var size = flame.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.2f)));
            var pr = engine.GetComponent<ParticleSystemRenderer>();
            pr.sharedMaterial = flameMaterial;
            pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            go.SetActive(false);
            return new Ship { transform = go.transform, trail = trail, flame = flame };
        }

        private void Update()
        {
            if (viewCamera == null) return;
            bool dimmed = dimWith != null && dimBase > 0f && dimWith.intensity < dimBase * 0.5f;
            float dt = Time.deltaTime;
            foreach (var ship in ships)
            {
                if (!ship.flying)
                {
                    ship.wait -= dt;
                    if (ship.wait <= 0f && !dimmed) Launch(ship);
                    continue;
                }
                if (dimmed) { Land(ship); continue; }
                Fly(ship, dt);
            }
        }

        private void Launch(Ship ship)
        {
            Transform cam = viewCamera.transform;
            float h = viewCamera.orthographic ? viewCamera.orthographicSize : 10f;
            float w = h * viewCamera.aspect;
            float depth = Random.Range(depthRange.x, depthRange.y);
            float margin = 6f;
            // Enter from one side of the view, leave by the opposite one, at slightly different heights.
            bool horizontal = Random.value < 0.75f;
            Vector3 a, b;
            if (horizontal)
            {
                a = new Vector3(-w - margin, Random.Range(-h, h), depth);
                b = new Vector3(w + margin, Random.Range(-h, h), depth + Random.Range(-10f, 10f));
            }
            else
            {
                a = new Vector3(Random.Range(-w, w), h + margin, depth);
                b = new Vector3(Random.Range(-w, w), -h - margin, depth + Random.Range(-10f, 10f));
            }
            if (Random.value < 0.5f) (a, b) = (b, a);
            Vector3 mid = (a + b) * 0.5f;
            Vector3 side = Vector3.Cross((b - a).normalized, Vector3.forward);
            Vector3 c = mid + side * Random.Range(-h, h) * 0.8f;

            ship.from = cam.TransformPoint(a);
            ship.control = cam.TransformPoint(c);
            ship.to = cam.TransformPoint(b);
            float length = Vector3.Distance(ship.from, ship.control) + Vector3.Distance(ship.control, ship.to);
            ship.duration = length / Random.Range(speedRange.x, speedRange.y);
            ship.t = 0f;
            ship.roll = 0f;
            float scale = Random.Range(scaleRange.x, scaleRange.y);
            ship.transform.localScale = Vector3.one * scale;
            ship.trail.widthMultiplier = scale * 3f;
            if (paints != null && paints.Length > 0)
                foreach (var r in ship.transform.GetComponentsInChildren<MeshRenderer>())
                    r.sharedMaterial = paints[Random.Range(0, paints.Length)];
            ship.transform.position = ship.from;
            ship.transform.rotation = Quaternion.LookRotation(Tangent(ship, 0f), cam.up);
            ship.transform.gameObject.SetActive(true);
            ship.trail.Clear();
            ship.flame.Play();
            ship.flying = true;
        }

        private void Fly(Ship ship, float dt)
        {
            Vector3 before = Tangent(ship, ship.t);
            ship.t += dt / ship.duration;
            if (ship.t >= 1f) { Land(ship); return; }
            Vector3 tangent = Tangent(ship, ship.t);
            ship.transform.position = Bezier(ship, ship.t);
            // Bank into the turn: roll follows how fast the heading swings, seen from the camera.
            float turn = Vector3.Dot(Vector3.Cross(before.normalized, tangent.normalized), viewCamera.transform.forward) / Mathf.Max(dt, 1e-4f);
            ship.roll = Mathf.Lerp(ship.roll, Mathf.Clamp(-turn * 60f, -40f, 40f), dt * 3f);
            ship.transform.rotation = Quaternion.LookRotation(tangent, viewCamera.transform.up) * Quaternion.Euler(0f, 0f, ship.roll);
        }

        private void Land(Ship ship)
        {
            ship.flying = false;
            ship.flame.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ship.transform.gameObject.SetActive(false);
            ship.wait = Random.Range(pauseRange.x, pauseRange.y);
        }

        private static Vector3 Bezier(Ship s, float t)
        {
            float u = 1f - t;
            return u * u * s.from + 2f * u * t * s.control + t * t * s.to;
        }

        private static Vector3 Tangent(Ship s, float t) =>
            2f * (1f - t) * (s.control - s.from) + 2f * t * (s.to - s.control);
    }
}
