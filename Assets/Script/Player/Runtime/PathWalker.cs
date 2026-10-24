using System;
using UnityEngine;

namespace Smoking.Player
{
    // Walks the character along a WalkPath automatically. Height comes from a downward
    // raycast onto the level geometry, so stairs built from colliders are climbed step by step.
    // If the ground disappears (or drops more than a step), the walker stops and raises LostGround.
    // A rise of more than a step, or a non-flat surface ahead, is a wall: the walker turns around.
    public sealed class PathWalker : MonoBehaviour
    {
        public enum EndMode { PingPong, Loop, Stop, WalkOff }

        [SerializeField] private WalkPath path;
        [SerializeField] private float speed = 1.4f;
        [Tooltip("WalkOff keeps going straight past the last waypoint, off the edge.")]
        [SerializeField] private EndMode endMode = EndMode.WalkOff;
        [SerializeField] private float turnSpeed = 540f;
        [Tooltip("After a wall turns the walker around, it backs off this far along the path and then tries again. 0: walks back until something else turns it.")]
        [SerializeField] private float retreatDistance = 2f;
        [Tooltip("Playtesting (Editor only): start from the path point nearest to where the character is placed, instead of the path start.")]
        [SerializeField] private bool startWherePlaced;

        [Header("Ground")]
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField] private float probeHeight = 1.5f;
        [SerializeField] private float probeDepth = 3f;
        [Tooltip("How fast the body rises / drops onto a new step (m/s).")]
        [SerializeField] private float stepSpeed = 2.5f;
        [Tooltip("Largest step the walker can climb; a deeper drop counts as falling off the edge.")]
        [SerializeField] private float maxStepHeight = 0.4f;
        [Tooltip("Surfaces steeper than this (e.g. a stair mid-rotation) are not walkable.")]
        [SerializeField] private float maxSlope = 20f;
        [Tooltip("How far in front of the body walls are detected, so the character stops before clipping in.")]
        [SerializeField] private float probeAhead = 0.25f;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [Tooltip("Ground speed at which the walk clip plays at 1x.")]
        [SerializeField] private float animationReferenceSpeed = 1.4f;

        private float distance;
        private int direction = 1;
        private bool stopped;
        private float groundHeight;
        // While backing off from a wall, the distance at which the walker heads forward again.
        private float retreatUntil = float.NegativeInfinity;

        public event Action LostGround;
        public Vector3 Velocity { get; private set; }

        private void Start()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator != null) animator.applyRootMotion = false;

            if (startWherePlaced && Application.isEditor) distance = path.ClosestDistance(transform.position);
            Vector3 start = path.Evaluate(distance, out Vector3 forward);
            groundHeight = start.y;
            if (TryGround(start, out float y)) groundHeight = y;
            transform.position = new Vector3(start.x, groundHeight, start.z);
            FaceInstant(forward);
        }

        private void Update()
        {
            if (path == null || path.PointCount < 2 || stopped) return;

            float previousDistance = distance;
            distance += speed * direction * Time.deltaTime;
            if (direction < 0 && distance <= retreatUntil)
            {
                direction = 1;
                retreatUntil = float.NegativeInfinity;
            }
            if (distance >= path.Length || distance <= 0f) HandlePathEnd();

            Vector3 target = path.Evaluate(distance, out Vector3 forward);
            if (distance > path.Length) target += forward * (distance - path.Length);
            forward *= direction;
            target.y = groundHeight;

            if (IsWallAhead(target, forward))
            {
                distance = previousDistance;
                retreatUntil = direction > 0 && retreatDistance > 0f ? distance - retreatDistance : float.NegativeInfinity;
                direction = -direction;
                return;
            }

            Velocity = forward * speed;
            if (!TryGround(target, out float groundY) || groundY < groundHeight - maxStepHeight)
            {
                transform.position = target;
                stopped = true;
                LostGround?.Invoke();
                return;
            }
            groundHeight = groundY;
            float y = Mathf.MoveTowards(transform.position.y, groundY, stepSpeed * Time.deltaTime);
            transform.position = new Vector3(target.x, y, target.z);

            Vector3 flat = new Vector3(forward.x, 0f, forward.z);
            if (flat.sqrMagnitude > 0.0001f)
            {
                Quaternion look = Quaternion.LookRotation(flat);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * Time.deltaTime);
            }

            if (animator != null) animator.speed = speed / animationReferenceSpeed;
        }

        private void HandlePathEnd()
        {
            switch (endMode)
            {
                case EndMode.PingPong:
                    distance = Mathf.Clamp(distance, 0f, path.Length);
                    direction = -direction;
                    retreatUntil = float.NegativeInfinity;
                    break;
                case EndMode.Loop:
                    distance = Mathf.Repeat(distance, path.Length);
                    break;
                case EndMode.Stop:
                    distance = Mathf.Clamp(distance, 0f, path.Length);
                    stopped = true;
                    if (animator != null) animator.speed = 0f;
                    break;
                case EndMode.WalkOff:
                    if (distance < 0f)
                    {
                        distance = 0f;
                        direction = 1;
                    }
                    break;
            }
        }

        // Ground just ahead that is too high or not flat blocks the way. A gap ahead is not a
        // wall: falling is decided under the body, so the character walks right up to the edge.
        private bool IsWallAhead(Vector3 point, Vector3 forward)
        {
            Vector3 flat = new Vector3(forward.x, 0f, forward.z).normalized;
            var ray = new Ray(point + flat * probeAhead + Vector3.up * probeHeight, Vector3.down);
            if (!Physics.Raycast(ray, out RaycastHit hit, probeHeight + probeDepth, groundMask, QueryTriggerInteraction.Ignore)) return false;
            return hit.point.y > groundHeight + maxStepHeight || !IsWalkable(hit);
        }

        private bool TryGround(Vector3 point, out float y)
        {
            var ray = new Ray(point + Vector3.up * probeHeight, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, probeHeight + probeDepth, groundMask, QueryTriggerInteraction.Ignore)
                && IsWalkable(hit))
            {
                y = hit.point.y;
                return true;
            }
            y = point.y;
            return false;
        }

        private bool IsWalkable(RaycastHit hit) => Vector3.Angle(hit.normal, Vector3.up) <= maxSlope;

        private void FaceInstant(Vector3 forward)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(forward);
        }
    }
}
