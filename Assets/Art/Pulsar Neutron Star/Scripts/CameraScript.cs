using System;
using UnityEngine;

namespace PulsarNeutronStar.Camera
{
    public class SimpleCameraController : MonoBehaviour
    {
        class CameraState
        {
            public float yaw;
            public float pitch;
            public float roll;
            public float x;
            public float y;
            public float z;

            public void SetFromTransform(Transform t)
            {
                pitch = t.eulerAngles.x;
                yaw = t.eulerAngles.y;
                roll = t.eulerAngles.z;
                x = t.position.x;
                y = t.position.y;
                z = t.position.z;
            }

            public void Translate(Vector3 translation)
            {
                Vector3 rotatedTranslation = Quaternion.Euler(pitch, yaw, roll) * translation;
                x += rotatedTranslation.x;
                y += rotatedTranslation.y;
                z += rotatedTranslation.z;
            }

            public void LerpTowards(CameraState target, float positionLerpPct, float rotationLerpPct)
            {
                yaw = Mathf.Lerp(yaw, target.yaw, rotationLerpPct);
                pitch = Mathf.Lerp(pitch, target.pitch, rotationLerpPct);
                roll = Mathf.Lerp(roll, target.roll, rotationLerpPct);

                x = Mathf.Lerp(x, target.x, positionLerpPct);
                y = Mathf.Lerp(y, target.y, positionLerpPct);
                z = Mathf.Lerp(z, target.z, positionLerpPct);
            }

            public void UpdateTransform(Transform t)
            {
                t.eulerAngles = new Vector3(pitch, yaw, roll);
                t.position = new Vector3(x, y, z);
            }
        }

        CameraState m_TargetCameraState = new CameraState();
        CameraState m_InterpolatingCameraState = new CameraState();

        [Header("Movement Settings")]
        [Tooltip("Base speed of camera movement")]
        public float movementSpeed = 10f;

        [Tooltip("Boost factor adjusted by the scroll wheel")]
        public float boost = 0.2f;

        [Tooltip("Time for smooth position interpolation")]
        public float positionLerpTime = 0.2f;

        [Header("Rotation Settings")]
        [Tooltip("Mouse sensitivity curve")]
        public AnimationCurve mouseSensitivityCurve = new AnimationCurve(new Keyframe(0f, 0.5f, 0f, 5f), new Keyframe(1f, 2.5f, 0f, 0f));

        [Tooltip("Time for smooth rotation interpolation")]
        public float rotationLerpTime = 0.01f;

        [Tooltip("Invert Y-axis for mouse movement")]
        public bool invertY = false;

        [Tooltip("Sensitivity adjustment amount with the scroll wheel")]
        public float scrollSensitivity = 0.1f;

        [Tooltip("Time for mouse sensitivity to reach full value")]
        public float mouseSensitivityAccelerationTime = 1f;

        private float currentMouseSensitivity = 0.1f;
        private float targetMouseSensitivity = 0.5f;
        private float maxMouseSensitivity;
        private float currentSpeedMultiplier = 1f;
        private float targetSpeedMultiplier = 1f;
        private float speedAdjustmentRate = 1f;

        void OnEnable()
        {
            m_TargetCameraState.SetFromTransform(transform);
            m_InterpolatingCameraState.SetFromTransform(transform);
            maxMouseSensitivity = mouseSensitivityCurve.Evaluate(1f);
        }

        Vector3 GetInputTranslationDirection()
        {
            Vector3 direction = new Vector3();
            if (Input.GetKey(KeyCode.W)) direction += Vector3.forward;
            if (Input.GetKey(KeyCode.S)) direction += Vector3.back;
            if (Input.GetKey(KeyCode.A)) direction += Vector3.left;
            if (Input.GetKey(KeyCode.D)) direction += Vector3.right;
            if (Input.GetKey(KeyCode.Q)) direction += Vector3.down;
            if (Input.GetKey(KeyCode.E)) direction += Vector3.up;
            return direction;
        }

        void Update()
        {
            Vector3 translation = Vector3.zero;

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKey(KeyCode.Escape))
            {
                Application.Quit();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            }

            if (Input.GetMouseButtonDown(1))
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
            if (Input.GetMouseButtonUp(1))
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.None;
            }

            // Gradual mouse sensitivity increase with inertia
            if (Input.GetMouseButton(1))
            {
                var mouseMovement = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y") * (invertY ? 1 : -1));

                // Calculate target sensitivity based on mouse movement
                targetMouseSensitivity = mouseSensitivityCurve.Evaluate(mouseMovement.magnitude);

                // Smooth transition of mouse sensitivity from the current to the target value
                currentMouseSensitivity = Mathf.Lerp(currentMouseSensitivity, targetMouseSensitivity, Time.deltaTime / mouseSensitivityAccelerationTime);

                // Apply smooth rotation to the target camera state
                m_TargetCameraState.yaw += mouseMovement.x * currentMouseSensitivity;
                m_TargetCameraState.pitch += mouseMovement.y * currentMouseSensitivity;
            }
            else
            {
                // Gradually reduce sensitivity when mouse button is released
                currentMouseSensitivity = Mathf.Lerp(currentMouseSensitivity, 0.1f, Time.deltaTime / mouseSensitivityAccelerationTime);
            }

            // Adjust mouse sensitivity with scroll wheel
            if (Input.mouseScrollDelta.y != 0)
            {
                currentMouseSensitivity = Mathf.Clamp(
                    currentMouseSensitivity + (Input.mouseScrollDelta.y * scrollSensitivity),
                    0.1f,
                    maxMouseSensitivity
                );
            }

            // Handle movement speed adjustments with Shift and Alt keys
            if (Input.GetKeyDown(KeyCode.LeftShift)) targetSpeedMultiplier += 1f;
            if (Input.GetKeyDown(KeyCode.LeftAlt)) targetSpeedMultiplier -= 1f;

            if (Input.GetKey(KeyCode.LeftShift))
                targetSpeedMultiplier += speedAdjustmentRate * Time.deltaTime;
            if (Input.GetKey(KeyCode.LeftAlt))
                targetSpeedMultiplier -= speedAdjustmentRate * Time.deltaTime;

            if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftAlt))
                targetSpeedMultiplier = Mathf.MoveTowards(targetSpeedMultiplier, 1f, speedAdjustmentRate * Time.deltaTime);

            targetSpeedMultiplier = Mathf.Max(1f, targetSpeedMultiplier);

            translation = GetInputTranslationDirection() * movementSpeed * targetSpeedMultiplier * Time.deltaTime;
            boost += Input.mouseScrollDelta.y * 0.2f;
            translation *= Mathf.Pow(2.0f, boost);

            m_TargetCameraState.Translate(translation);

            var positionLerpPct = 1f - Mathf.Exp((Mathf.Log(1f - 0.99f) / positionLerpTime) * Time.deltaTime);
            var rotationLerpPct = 1f - Mathf.Exp((Mathf.Log(1f - 0.99f) / rotationLerpTime) * Time.deltaTime);
            m_InterpolatingCameraState.LerpTowards(m_TargetCameraState, positionLerpPct, rotationLerpPct);

            m_InterpolatingCameraState.UpdateTransform(transform);
#endif
        }
    }
}
