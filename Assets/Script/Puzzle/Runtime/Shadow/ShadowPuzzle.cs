using System.Collections;
using System.Collections.Generic;
using Smoking.Audio;
using Smoking.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Smoking.Puzzle
{
    // The shadow sequence. When the player walks into the trigger box: the world dims, a spotlight
    // behind the character comes up and the camera turns to the wall. The character marches in
    // place while its shadow walks on across the shadows of the ShadowShapes; WASD moves the light
    // to reshape them. When the shadow reaches a goal shadow, the lights come back on, the camera
    // returns and the character walks on along its path.
    public sealed class ShadowPuzzle : MonoBehaviour
    {
        [Header("Trigger")]
        [Tooltip("Box around this transform (local space). The sequence starts when the player walks in.")]
        [SerializeField] private Vector3 areaSize = new Vector3(1.5f, 3f, 1.5f);
        [Tooltip("Left empty: the scene object named \"Player\".")]
        [SerializeField] private PathWalker player;

        [Header("Scene")]
        [SerializeField] private ShadowWall wall;
        [SerializeField] private Light spotlight;
        [Tooltip("The spotlight keeps pointing at this, e.g. the middle of the puzzle on the wall. Empty: straight at the wall.")]
        [SerializeField] private Transform lightAim;
        [Tooltip("Left empty: every ShadowShape in the scene.")]
        [SerializeField] private List<ShadowShape> shapes = new List<ShadowShape>();
        [Tooltip("A CinemachineCamera framing the wall; enabled for the sequence, so the brain blends to it.")]
        [SerializeField] private GameObject shadowCamera;
        [Tooltip("Turned when the puzzle is solved.")]
        [SerializeField] private Transform lever;
        [SerializeField] private Vector3 leverTurn = new Vector3(0f, 0f, -70f);
        [Tooltip("Seconds the shadow world stays on screen after the lever turns, so the player sees it.")]
        [SerializeField] private float leverHoldTime = 1.6f;

        [Header("Light control (WASD / arrows)")]
        [SerializeField] private float moveSpeed = 3f;
        [Tooltip("How far the light may slide along the wall from its start (A / D).")]
        [SerializeField] private Vector2 sideRange = new Vector2(-8f, 8f);
        [Tooltip("How far the light may move toward (+) or away from (-) the wall from its start (W / S).")]
        [SerializeField] private Vector2 depthRange = new Vector2(-4f, 4f);
        [Tooltip("The light always stays at least this far behind the character.")]
        [SerializeField] private float minBehindPlayer = 0.75f;

        [Header("Atmosphere")]
        [SerializeField] private float fadeTime = 2f;
        [SerializeField] private float spotIntensity = 900f;
        [SerializeField, Range(0f, 1f)] private float darkAmbient = 0.05f;
        [SerializeField] private Color darkBackground = new Color(0.05f, 0.05f, 0.07f);

        [Header("Shadow walker")]
        [SerializeField] private float characterHeight = 1.7f;
        [Tooltip("Walking speed of the character the shadow belongs to (m/s); the shadow moves this times its magnification.")]
        [SerializeField] private float walkSpeed = 1.2f;
        [Tooltip("Tallest climbable step, as a fraction of the shadow's height.")]
        [SerializeField, Range(0.05f, 0.5f)] private float stepRatio = 0.25f;
        [Tooltip("A fall deeper than this many body heights below the start counts as a fail.")]
        [SerializeField] private float failDepth = 1.5f;
        [SerializeField] private float respawnDelay = 1f;

        [Header("Camera follow")]
        [SerializeField] private float cameraDistance = 20f;
        [Tooltip("Height of the camera's center above the shadow's start, in wall metres.")]
        [SerializeField] private float cameraHeight = 3f;
        [SerializeField] private float cameraFollowTime = 0.6f;

        [Header("Sounds")]
        [Tooltip("The world goes dark as the sequence starts.")]
        [SerializeField] private AudioCue lightsOffSound;
        [Tooltip("The shadow flips the lever.")]
        [SerializeField] private AudioCue lightsOnSound;
        [SerializeField] private AudioCue fallSound;
        [SerializeField] private AudioCue solvedSound;

        private enum State { Waiting, Playing, Done }

        private readonly Dictionary<Renderer, UnityEngine.Rendering.ShadowCastingMode> castModes =
            new Dictionary<Renderer, UnityEngine.Rendering.ShadowCastingMode>();
        private readonly List<(Light light, float intensity)> worldLights = new List<(Light, float)>();
        private State state;
        private ShadowWalker walker;
        private Vector3 lightStart;
        private float ambient;
        private Color background;
        private Camera viewCamera;
        private float cameraX, cameraVelocity, cameraY;
        private bool walking;

        private void Start()
        {
            if (player == null)
            {
                var found = GameObject.Find("Player");
                if (found != null) player = found.GetComponent<PathWalker>();
            }
            if (shapes.Count == 0) shapes.AddRange(FindObjectsByType<ShadowShape>());
            viewCamera = Camera.main;
            lightStart = spotlight.transform.position;
            AimLight();
            spotlight.intensity = 0f;
            spotlight.enabled = false;
            if (shadowCamera != null) shadowCamera.SetActive(false);
        }

        private void Update()
        {
            if (state == State.Waiting && player != null && Contains(player.transform.position)) Begin();
            if (state != State.Playing) return;

            MoveLight(Time.deltaTime);
            foreach (var shape in shapes) shape.Rebuild(wall, spotlight.transform.position);
            if (walking) walker.Tick(Time.deltaTime);
            FollowShadow(Time.deltaTime);
        }

        private void Begin()
        {
            state = State.Playing;
            player.enabled = false; // the animator keeps playing the walk: the character marches in place
            AudioBootstrap.SharedSfx?.Play(lightsOffSound);

            // Only the puzzle casts shadows now; the floor, the character itself and everything else would clutter the wall.
            // The puzzle pieces themselves turn invisible but keep their shadows, so they don't block the view of the wall.
            var shapeRenderers = new HashSet<Renderer>();
            foreach (var shape in shapes) shapeRenderers.Add(shape.GetComponent<Renderer>());
            foreach (var r in FindObjectsByType<Renderer>())
            {
                castModes[r] = r.shadowCastingMode;
                r.shadowCastingMode = shapeRenderers.Contains(r)
                    ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly
                    : UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            walker = CreatePuppet(player.GetComponentInChildren<Animator>().transform);
            walker.Init(wall, spotlight.transform, shapes, player.transform.position, characterHeight, walkSpeed, stepRatio, failDepth);
            walker.Fell += HandleFell;
            walker.ReachedGoal += HandleGoal;

            foreach (var shape in shapes) shape.Rebuild(wall, spotlight.transform.position);
            walker.Spawn();
            cameraX = walker.Position.x;
            cameraY = walker.Position.y + cameraHeight;
            FollowShadow(0f);
            if (shadowCamera != null) shadowCamera.SetActive(true);
            StartCoroutine(BeginAfterFade());
        }

        private IEnumerator BeginAfterFade()
        {
            worldLights.Clear();
            foreach (var l in FindObjectsByType<Light>())
                if (l != spotlight && l.enabled) worldLights.Add((l, l.intensity));
            ambient = RenderSettings.ambientIntensity;
            background = viewCamera.backgroundColor;
            spotlight.enabled = true;
            yield return Fade(0f, 1f);
            walking = true;
        }

        private void HandleFell()
        {
            if (!walking) return;
            walking = false;
            AudioBootstrap.SharedSfx?.Play(fallSound);
            StartCoroutine(Respawn());
        }

        private IEnumerator Respawn()
        {
            walker.gameObject.SetActive(false);
            yield return new WaitForSeconds(respawnDelay);
            walker.gameObject.SetActive(true);
            walker.Spawn();
            walking = true;
        }

        private void HandleGoal()
        {
            if (state != State.Playing) return;
            state = State.Done;
            walking = false;
            AudioBootstrap.SharedSfx?.Play(solvedSound);
            StartCoroutine(Finish());
        }

        private IEnumerator Finish()
        {
            AudioBootstrap.SharedSfx?.Play(lightsOnSound);
            if (lever != null)
            {
                Quaternion from = lever.localRotation;
                StartCoroutine(RotatableStair.Turn(lever, from, from * Quaternion.Euler(leverTurn), 0.3f));
            }
            yield return new WaitForSeconds(leverHoldTime);
            if (shadowCamera != null) shadowCamera.SetActive(false);
            yield return Fade(1f, 0f);
            spotlight.enabled = false;
            Destroy(walker.gameObject);
            foreach (var pair in castModes) if (pair.Key != null) pair.Key.shadowCastingMode = pair.Value;
            castModes.Clear();
            player.enabled = true;
        }

        // 0 = normal world, 1 = dark world lit by the spotlight.
        private IEnumerator Fade(float from, float to)
        {
            for (float t = 0f; t < fadeTime; t += Time.deltaTime)
            {
                ApplyDarkness(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / fadeTime)));
                yield return null;
            }
            ApplyDarkness(to);
        }

        private void ApplyDarkness(float k)
        {
            foreach (var (l, intensity) in worldLights) if (l != null) l.intensity = Mathf.Lerp(intensity, 0f, k);
            RenderSettings.ambientIntensity = Mathf.Lerp(ambient, darkAmbient, k);
            viewCamera.backgroundColor = Color.Lerp(background, darkBackground, k);
            spotlight.intensity = spotIntensity * k;
        }

        private void MoveLight(float dt)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            float side = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
                       - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
            float depth = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f)
                        - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
            if (side == 0f && depth == 0f) return;

            Vector3 toWall = -wall.Normal;
            Vector3 offset = spotlight.transform.position - lightStart;
            float x = Mathf.Clamp(Vector3.Dot(offset, wall.Right) + side * moveSpeed * dt, sideRange.x, sideRange.y);
            float maxDepth = Mathf.Min(depthRange.y, Vector3.Dot(player.transform.position - lightStart, toWall) - minBehindPlayer);
            float z = Mathf.Clamp(Vector3.Dot(offset, toWall) + depth * moveSpeed * dt, depthRange.x, maxDepth);
            float y = Vector3.Dot(offset, Vector3.up);
            spotlight.transform.position = lightStart + wall.Right * x + toWall * z + Vector3.up * y;
            AimLight();
        }

        private void AimLight()
        {
            Vector3 direction = lightAim != null ? lightAim.position - spotlight.transform.position : -wall.Normal;
            spotlight.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        private void FollowShadow(float dt)
        {
            if (shadowCamera == null || walker == null) return;
            if (dt > 0f) cameraX = Mathf.SmoothDamp(cameraX, walker.Position.x, ref cameraVelocity, cameraFollowTime, Mathf.Infinity, dt);
            Vector3 center = wall.ToWorld(new Vector2(cameraX, cameraY));
            shadowCamera.transform.SetPositionAndRotation(center + wall.Normal * cameraDistance, Quaternion.LookRotation(-wall.Normal, wall.Up));
        }

        // A shadow-only copy of the character model with its physics stripped, carrying the ShadowWalker.
        private ShadowWalker CreatePuppet(Transform model)
        {
            var holder = new GameObject("ShadowPuppet (building)");
            holder.SetActive(false); // keeps the copy's Awake (ragdoll building) from running
            var copy = Instantiate(model.gameObject, holder.transform);
            foreach (var c in copy.GetComponentsInChildren<Joint>(true)) DestroyImmediate(c);
            foreach (var c in copy.GetComponentsInChildren<Rigidbody>(true)) DestroyImmediate(c);
            foreach (var c in copy.GetComponentsInChildren<Collider>(true)) DestroyImmediate(c);
            foreach (var c in copy.GetComponentsInChildren<MonoBehaviour>(true)) DestroyImmediate(c);
            foreach (var r in copy.GetComponentsInChildren<Renderer>(true))
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;

            var root = new GameObject("ShadowPuppet");
            copy.transform.SetParent(root.transform, false);
            copy.transform.localPosition = Vector3.zero;
            copy.transform.localRotation = Quaternion.identity;
            Destroy(holder);
            return root.AddComponent<ShadowWalker>();
        }

        private bool Contains(Vector3 point)
        {
            Vector3 local = transform.InverseTransformPoint(point);
            Vector3 half = areaSize * 0.5f;
            return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z;
        }

        private void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 0.9f, 0.4f, 0.08f);
            Gizmos.DrawCube(Vector3.zero, areaSize);
            Gizmos.color = new Color(1f, 0.9f, 0.4f, 0.9f);
            Gizmos.DrawWireCube(Vector3.zero, areaSize);
        }
    }
}
