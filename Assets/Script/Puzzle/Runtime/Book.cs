using System;
using System.Collections;
using UnityEngine;

namespace Smoking.Puzzle
{
    // One flying book. Wanders inside its BookSwarm's flight volume and reports clicks back to it.
    public sealed class Book : MonoBehaviour, IClickable
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private BookSwarm swarm;
        private Vector3 velocity;
        private Vector3 target;
        private Vector3 spinAxis;
        private float speed;
        private float spinSpeed;
        private float baseScale;
        private bool busy;

        public Color Tint { get; private set; }

        // glow: how much the book lights itself in its own color, so dark or tinted scenes can't shift it.
        public void Init(BookSwarm owner, Color color, float scale, float minSpeed, float maxSpeed, float glow)
        {
            swarm = owner;
            Tint = color;
            baseScale = scale;
            speed = UnityEngine.Random.Range(minSpeed, maxSpeed);
            spinSpeed = UnityEngine.Random.Range(40f, 120f);
            spinAxis = UnityEngine.Random.onUnitSphere;
            transform.localRotation = UnityEngine.Random.rotation;
            PickTarget();

            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColorId, new Color(color.r, color.g, color.b, 1f));
            block.SetColor(EmissionColorId, new Color(color.r, color.g, color.b, 1f) * glow);
            GetComponent<Renderer>().SetPropertyBlock(block);
            StartCoroutine(ScaleTo(0f, scale, 0.3f));
        }

        public void OnClicked(RaycastHit hit)
        {
            if (!busy) swarm.HandleClick(this);
        }

        private void Update()
        {
            if (busy) return;
            Vector3 position = transform.localPosition;
            Vector3 goal = Vector3.Scale(target, swarm.Extents);
            if ((goal - position).sqrMagnitude < 0.25f) PickTarget();

            velocity = Vector3.MoveTowards(velocity, (goal - position).normalized * speed, speed * 1.5f * Time.deltaTime);
            transform.localPosition = position + velocity * Time.deltaTime;
            transform.localRotation *= Quaternion.AngleAxis(spinSpeed * Time.deltaTime, spinAxis);
        }

        // Right book: fly into the segment's hint, then let it turn solid.
        public void FlyTo(Func<Vector3> destination, Action arrived)
        {
            busy = true;
            GetComponent<Collider>().enabled = false;
            transform.SetParent(null, true);
            StartCoroutine(Fly(destination, arrived));
        }

        // Wrong book: a quick shake so the click still feels acknowledged.
        public void Reject()
        {
            if (!busy) StartCoroutine(Shake());
        }

        // Puzzle solved: drift up and away.
        public void Leave()
        {
            busy = true;
            GetComponent<Collider>().enabled = false;
            StartCoroutine(FlyAway());
        }

        private void PickTarget() =>
            target = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f));

        private IEnumerator Fly(Func<Vector3> destination, Action arrived)
        {
            Vector3 start = transform.position;
            const float duration = 0.6f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / duration);
                transform.position = Vector3.Lerp(start, destination(), k);
                transform.localScale = Vector3.one * Mathf.Lerp(baseScale, baseScale * 0.3f, k);
                transform.Rotate(Vector3.up, 720f * Time.deltaTime, Space.World);
                yield return null;
            }
            arrived?.Invoke();
            Destroy(gameObject);
        }

        private IEnumerator Shake()
        {
            busy = true;
            Quaternion rotation = transform.localRotation;
            const float duration = 0.3f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float falloff = 1f - t / duration;
                transform.localRotation = rotation * Quaternion.Euler(0f, 0f, Mathf.Sin(t * 60f) * 25f * falloff);
                yield return null;
            }
            transform.localRotation = rotation;
            busy = false;
        }

        private IEnumerator FlyAway()
        {
            Vector3 drift = (Vector3.up + UnityEngine.Random.insideUnitSphere * 0.5f) * 8f;
            const float duration = 1.2f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                transform.position += drift * Time.deltaTime;
                transform.localScale = Vector3.one * Mathf.Lerp(baseScale, 0f, t / duration);
                yield return null;
            }
            Destroy(gameObject);
        }

        private IEnumerator ScaleTo(float from, float to, float duration)
        {
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                transform.localScale = Vector3.one * Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            transform.localScale = Vector3.one * to;
        }
    }
}
