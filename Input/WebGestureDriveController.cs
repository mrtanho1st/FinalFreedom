#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting;

namespace FinalFreedom
{
    // Same game-facing API as the native controller; browser camera and AI live in ar/.
    public sealed class GestureDriveController : MonoBehaviour
    {
        [DllImport("__Internal")] static extern void FFHandsStart(string receiver, int session);
        [DllImport("__Internal")] static extern void FFHandsStop();
        [DllImport("__Internal")] static extern int FFHandsCopyFrame(byte[] target, int length);
        public bool Selected { get; private set; }
        public bool Ready => running && interpreter.Calibrated && interpreter.TwoHands &&
            Time.unscaledTime - lastGoodHands < 0.4f && string.IsNullOrEmpty(Error);
        public bool HoldingNitro => Selected && Ready && interpreter.Nitro && Time.unscaledTime - lastResult < 0.25f;
        public string Error { get; private set; }
        public Texture2D Preview { get; private set; }
        public TrackedHand[] Hands { get; private set; } = Array.Empty<TrackedHand>();
        public HandGestureInterpreter Interpreter => interpreter;
        public string Status => !string.IsNullOrEmpty(Error) ? Error : !running ? "Allow camera access. Loading hand AI..." :
            !interpreter.TwoHands ? "Keep both hands visible" : !interpreter.Calibrated ?
            "Hold two relaxed fists level: " + Mathf.RoundToInt(interpreter.CalibrationProgress * 100) + "%" :
            game.UI.IsGestureSetup ? (interpreter.StartProgress > 0 ? "Hold thumb up: " + Mathf.RoundToInt(interpreter.StartProgress * 100) + "%" :
                "READY - thumb up to start; keep both hands visible") :
            interpreter.Brake ? "BRAKE" : interpreter.Nitro ? "NITRO" : "READY - rotate hands to steer";
        readonly HandGestureInterpreter interpreter = new HandGestureInterpreter();
        GameManager game;
        bool running, starting;
        int session;
        float lastGoodHands, lastResult, startedAt, nextPreview;
        byte[] previewBytes;
        [Serializable] sealed class BrowserResult
        {
            public int session, width, height;
            public float ageMs;
            public TrackedHand[] hands;
            public string error;
        }
        public void Initialize(GameManager manager) { game = manager; }
        public void UseTouch() { Selected = false; StopCamera(); game.Input.Clear(); }
        public void ClearCommands() { interpreter.ClearCommands(); }
        public void StartTracking()
        {
            StopCamera(); Selected = true; Error = null; starting = true;
            startedAt = lastGoodHands = lastResult = Time.unscaledTime;
            FFHandsStart(gameObject.name, session);
        }
        [Preserve] public void OnWebHandsReady(string json)
        {
            var message = JsonUtility.FromJson<BrowserResult>(json);
            if (!Selected || message.session != session) return;
            running = true; starting = false;
            lastGoodHands = lastResult = Time.unscaledTime;
        }
        [Preserve] public void OnWebHandsError(string json)
        {
            var message = JsonUtility.FromJson<BrowserResult>(json);
            if (message.session != session || !Selected) return;
            Fail(message.error);
        }
        [Preserve] public void OnWebHands(string json)
        {
            var message = JsonUtility.FromJson<BrowserResult>(json);
            if (!running || message.session != session || !Selected) return;
            if (message.width < 1 || message.height < 1 || message.width > 384 || message.height > 384) return;
            if (Preview == null || Preview.width != message.width || Preview.height != message.height)
            {
                if (Preview != null)
                {
                    Destroy(Preview); interpreter.ResetCalibration();
                    if (game.IsPlaying) { game.TogglePause(); return; }
                }
                Preview = new Texture2D(message.width, message.height, TextureFormat.RGBA32, false);
                previewBytes = new byte[message.width * message.height * 4];
            }
            Hands = message.ageMs < 400 ? message.hands ?? Array.Empty<TrackedHand>() : Array.Empty<TrackedHand>();
            foreach (var hand in Hands)
            {
                if (hand?.points == null || hand.points.Length != 21) { Hands = Array.Empty<TrackedHand>(); break; }
                foreach (var p in hand.points)
                    if (float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsInfinity(p.x) || float.IsInfinity(p.y))
                    { Hands = Array.Empty<TrackedHand>(); break; }
            }
            float now = Time.unscaledTime;
            interpreter.Process(Hands, now - lastResult, !game.IsPlaying && game.UI.IsGestureSetup);
            lastResult = now;
            if (interpreter.TwoHands) lastGoodHands = now;
            if (game.IsPlaying && interpreter.Calibrated)
            {
                if (interpreter.Brake) game.Player.Brake();
                if (interpreter.LaneStep != 0) game.Player.MoveLane(interpreter.LaneStep);
            }
        }
        void Update()
        {
            if (!Selected) return;
            if (starting && Time.unscaledTime - startedAt > 60) { Fail("Camera/AI timed out. Allow access and retry, or use touch."); return; }
            if (game.IsPlaying && (Time.unscaledTime - lastGoodHands > 0.8f || !string.IsNullOrEmpty(Error)))
            { game.TogglePause(); game.UI.Toast("Tracking lost - recalibrate to resume", 4); return; }
            if (running && Preview != null && Time.unscaledTime >= nextPreview)
            {
                nextPreview = Time.unscaledTime + 1f / 15;
                if (FFHandsCopyFrame(previewBytes, previewBytes.Length) != 0)
                { Preview.LoadRawTextureData(previewBytes); Preview.Apply(false, false); }
            }
        }
        void Fail(string message)
        {
            Error = message; StopCamera();
            if (game != null && game.IsPlaying) game.TogglePause();
        }
        public void StopCamera()
        {
            session++; FFHandsStop(); running = starting = false;
            if (Preview != null) Destroy(Preview);
            Preview = null; previewBytes = null; Hands = Array.Empty<TrackedHand>(); interpreter.ResetCalibration();
        }
        void OnApplicationPause(bool paused) { if (paused && Selected && running) Fail("Camera paused. Tap RETRY CAMERA."); }
        void OnApplicationFocus(bool focused) { if (!focused && Selected && running) Fail("Camera paused. Tap RETRY CAMERA."); }
        void OnDestroy() { StopCamera(); }
    }
}
#endif
