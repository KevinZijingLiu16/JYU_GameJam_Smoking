using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Smoking.Puzzle
{
    public interface IClickable
    {
        void OnClicked(RaycastHit hit);
    }

    // Mouse / touch picking for puzzle pieces: raycasts from the camera and forwards the hit
    // to the nearest IClickable on the hit object or its parents.
    public sealed class ClickInput : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private LayerMask clickMask = ~0;
        [SerializeField] private float maxDistance = 200f;

        private void Awake()
        {
            if (viewCamera == null) viewCamera = Camera.main;
        }

        private void Update()
        {
            var pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            Ray ray = viewCamera.ScreenPointToRay(pointer.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, clickMask, QueryTriggerInteraction.Collide)) return;
            hit.collider.GetComponentInParent<IClickable>()?.OnClicked(hit);
        }
    }
}
