using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace FinalFreedom
{
    public sealed class SwipeManager : MonoBehaviour
    {
        public bool HoldingNitro { get; private set; }
        public bool NitroButtonHeld { get; set; }
        readonly List<RaycastResult> hits = new List<RaycastResult>();
        Vector2 origin;
        float started;
        bool tracking, consumed, blocked;
        public void Clear() { tracking = consumed = blocked = HoldingNitro = NitroButtonHeld = false; }
        bool OverButton(Vector2 position)
        {
            if (EventSystem.current == null) return false;
            var data = new PointerEventData(EventSystem.current) { position = position };
            hits.Clear(); EventSystem.current.RaycastAll(data, hits);
            return hits.Count > 0;
        }
        void Update()
        {
            var game = GameManager.Instance;
            if (game == null) return;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.escapeKey.wasPressedThisFrame) game.TogglePause();
                if (keyboard.rKey.wasPressedThisFrame && game.State == RunState.GameOver) game.BeginRun();
            }
            if (!game.IsPlaying) { HoldingNitro = false; return; }
            if (keyboard != null)
            {
                if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame) game.Player.MoveLane(-1);
                if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame) game.Player.MoveLane(1);
                if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame) game.Player.Brake();
            }
            bool pressed = false, down = false;
            Vector2 position = default;
            var touch = Touchscreen.current;
            if (touch != null && (touch.primaryTouch.press.isPressed || touch.primaryTouch.press.wasReleasedThisFrame))
            {
                pressed = touch.primaryTouch.press.wasPressedThisFrame;
                down = touch.primaryTouch.press.isPressed;
                position = touch.primaryTouch.position.ReadValue();
            }
            else if (Mouse.current != null)
            {
                pressed = Mouse.current.leftButton.wasPressedThisFrame;
                down = Mouse.current.leftButton.isPressed;
                position = Mouse.current.position.ReadValue();
            }
            if (pressed)
            {
                origin = position; started = Time.unscaledTime; consumed = false;
                blocked = OverButton(position); tracking = !blocked;
            }
            if (tracking && down && !consumed)
            {
                Vector2 delta = position - origin;
                float threshold = Mathf.Max(26, Mathf.Min(Screen.width, Screen.height) * 0.055f);
                if (delta.magnitude > threshold)
                {
                    if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y)) game.Player.MoveLane(delta.x > 0 ? 1 : -1);
                    else if (delta.y < 0) game.Player.Brake();
                    consumed = true;
                }
            }
            HoldingNitro = NitroButtonHeld || (keyboard != null && keyboard.spaceKey.isPressed)
                || (tracking && down && !consumed && Time.unscaledTime - started > 0.22f);
            if (!down) tracking = false;
        }
        void OnApplicationFocus(bool focus) { if (!focus) Clear(); }
    }
}
