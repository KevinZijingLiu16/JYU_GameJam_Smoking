using System;
using System.Collections.Generic;
using UnityEngine;

namespace Smoking.Puzzle
{
    // The shadow version of the player: walks on its own across the shadows on the wall, like
    // PathWalker does in 3D. Steps up or down onto shadows within a step, turns around at taller
    // ones, falls through gaps. It is drawn by a shadow-only copy of the character placed at the
    // character's depth, so its shadow lands where the walker is and scales like the real one.
    public sealed class ShadowWalker : MonoBehaviour
    {
        private ShadowWall wall;
        private Transform lightSource;
        private IReadOnlyList<ShadowShape> shapes;
        private Vector3 anchor;
        private float bodyHeight, speed, stepRatio, failDepth;

        private Vector2 position;
        private float spawnHeight;
        private float fallSpeed;
        private int direction = 1;
        private int startDirection = 1;
        private bool falling;

        public event Action Fell;
        public event Action ReachedGoal;

        // Wall-space height of the shadow body at the current light position.
        private float Height => bodyHeight * Scale;
        private float Scale => wall.Magnification(lightSource.position, anchor);

        // anchor: the real character's feet; the puppet stays at this depth.
        public void Init(ShadowWall shadowWall, Transform light, IReadOnlyList<ShadowShape> shadowShapes,
            Vector3 feet, float characterHeight, float walkSpeed, float maxStepRatio, float fallLimit)
        {
            wall = shadowWall;
            lightSource = light;
            shapes = shadowShapes;
            anchor = feet;
            bodyHeight = characterHeight;
            speed = walkSpeed;
            stepRatio = maxStepRatio;
            failDepth = fallLimit;
        }

        // 1: sets off along the wall's Right (screen-right for the shadow camera); -1: the other way.
        public void SetStartDirection(int sign) => startDirection = sign < 0 ? -1 : 1;

        // Starts again at the real character's feet, as if the shadow just stepped away from it.
        public bool Spawn()
        {
            if (!wall.TryProject(lightSource.position, anchor, out position)) return false;
            spawnHeight = position.y;
            falling = false;
            fallSpeed = 0f;
            direction = startDirection;
            Place();
            return true;
        }

        public void Tick(float dt)
        {
            float height = Height;
            float step = height * stepRatio;

            if (falling) Fall(dt, height);
            else Walk(dt, height, step);
            Place();
        }

        private void Walk(float dt, float height, float step)
        {
            // Shadows slide as the light moves: one that swallows the feet lifts the walker onto it.
            if (Inside(position.x, position.y + 0.02f, out float lift)) position.y = lift;

            float move = speed * Scale * dt;
            float ahead = position.x + direction * (move + height * 0.15f);
            if (Blocked(ahead, position.y + step, position.y + height * 0.9f))
            {
                direction = -direction;
                return;
            }

            position.x += direction * move;
            if (Ground(position.x, position.y + step, out float ground, out ShadowShape under) && ground >= position.y - step)
            {
                position.y = ground;
                if (under.Goal) ReachedGoal?.Invoke();
            }
            else falling = true;
        }

        private void Fall(float dt, float height)
        {
            fallSpeed += 9.81f * Scale * dt;
            float next = position.y - fallSpeed * dt;
            if (Ground(position.x, position.y, out float ground, out _) && ground >= next)
            {
                position.y = ground;
                falling = false;
                fallSpeed = 0f;
                return;
            }
            position.y = next;
            if (position.y < spawnHeight - failDepth * height) Fell?.Invoke();
        }

        // Highest solid shadow top at x that is not above `limit`.
        private bool Ground(float x, float limit, out float top, out ShadowShape shape)
        {
            top = float.MinValue;
            shape = null;
            foreach (var s in shapes)
            {
                if (!s.Solid || !s.SpanAt(x, out _, out float t) || t > limit || t <= top) continue;
                top = t;
                shape = s;
            }
            return shape != null;
        }

        private bool Inside(float x, float y, out float top)
        {
            top = float.MinValue;
            foreach (var s in shapes)
                if (s.Solid && s.SpanAt(x, out float b, out float t) && b < y && t > y) top = Mathf.Max(top, t);
            return top > float.MinValue;
        }

        private bool Blocked(float x, float from, float to)
        {
            foreach (var s in shapes)
                if (s.Solid && s.SpanAt(x, out float b, out float t) && t > from && b < to) return true;
            return false;
        }

        // Put the shadow-only puppet on the light ray through the walker's feet, at the anchor's depth.
        private void Place()
        {
            Vector3 light = lightSource.position;
            Vector3 feet = wall.ToWorld(position);
            float k = Vector3.Dot(anchor - light, wall.Normal) / Vector3.Dot(feet - light, wall.Normal);
            transform.position = light + (feet - light) * k;
            transform.rotation = Quaternion.LookRotation(wall.Right * direction, Vector3.up);
        }

        public Vector2 Position => position;
    }
}
