using UnityEngine;
using UnityEngine.EventSystems;

namespace FinalFreedom
{
    public sealed class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public void OnPointerDown(PointerEventData data) { if (GameManager.Instance != null) GameManager.Instance.Input.NitroButtonHeld = true; }
        public void OnPointerUp(PointerEventData data) { Release(); }
        public void OnPointerExit(PointerEventData data) { Release(); }
        void OnDisable() { Release(); }
        void Release() { if (GameManager.Instance != null) GameManager.Instance.Input.NitroButtonHeld = false; }
    }
}
