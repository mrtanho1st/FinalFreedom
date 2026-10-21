#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace FinalFreedom
{
    public sealed class GestureDriveController : MonoBehaviour
    {
        public bool Selected { get; private set; }
        public bool Ready => cameraTexture != null && cameraTexture.isPlaying && interpreter.Calibrated && interpreter.TwoHands &&
            Time.unscaledTime - lastGoodHands < 0.4f && string.IsNullOrEmpty(Error);
        public bool HoldingNitro => Selected && Ready && interpreter.Nitro && Time.unscaledTime - lastResult < 0.25f;
        public string Error { get; private set; }
        public Texture2D Preview { get; private set; }
        public TrackedHand[] Hands { get; private set; } = Array.Empty<TrackedHand>();
        public HandGestureInterpreter Interpreter => interpreter;
        public string Status => !string.IsNullOrEmpty(Error) ? Error : cameraTexture == null ? "Starting front camera..." :
            !interpreter.TwoHands ? "Keep both hands visible" : !interpreter.Calibrated ?
            "Hold two relaxed fists level: " + Mathf.RoundToInt(interpreter.CalibrationProgress * 100) + "%" :
            game.UI.IsGestureSetup ? (interpreter.StartProgress > 0 ?
                "Hold thumb up: " + Mathf.RoundToInt(interpreter.StartProgress * 100) + "%" :
                "READY - thumb up to start; keep both hands visible") :
            interpreter.Brake ? "BRAKE" : interpreter.Nitro ? "NITRO" : "READY - rotate hands to steer";
        readonly HandGestureInterpreter interpreter = new HandGestureInterpreter();
        GameManager game;
        WebCamTexture cameraTexture;
        HandLandmarkEngine engine;
        Task<TrackedHand[]> inference;
        Coroutine startup;
        NativeArray<byte> pixels;
        Color32[] source, display;
        float nextFrame, lastResult, lastGoodHands, capturedAt;
        long timestamp;
        int previousRotation = -1;
        bool requestingPermission;

        public void Initialize(GameManager manager) { game = manager; }
        public void UseTouch()
        {
            Selected = false; StopCamera();
            game.Input.Clear();
        }
        public void StartTracking()
        {
            StopCamera(); Selected = true; Error = null;
            interpreter.ResetCalibration();
            lastGoodHands = lastResult = Time.unscaledTime;
            startup = StartCoroutine(OpenCamera());
        }
        IEnumerator OpenCamera()
        {
            requestingPermission = true;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                bool answered = false;
                var callbacks = new PermissionCallbacks();
                callbacks.PermissionGranted += _ => answered = true;
                callbacks.PermissionDenied += _ => answered = true;
                Permission.RequestUserPermission(Permission.Camera, callbacks);
                float until = Time.realtimeSinceStartup + 30;
                while (!answered && Time.realtimeSinceStartup < until) yield return null;
                if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
                { requestingPermission = false; Error = "Camera permission denied. Enable it in app settings, or use touch controls."; yield break; }
            }
#else
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            { requestingPermission = false; Error = "Camera access denied. Allow camera access or use touch controls."; yield break; }
#endif
            requestingPermission = false;
            var devices = WebCamTexture.devices;
            int index = Array.FindIndex(devices, device => device.isFrontFacing);
            if (index < 0 && !Application.isMobilePlatform && devices.Length > 0) index = 0;
            if (index < 0) { Error = "No front camera found. Connect a webcam or use touch controls."; yield break; }
            try
            {
                var model = Resources.Load<TextAsset>("AI/hand_landmarker");
                if (model == null) throw new InvalidOperationException("Missing hand landmark model.");
                engine = new HandLandmarkEngine(model.bytes);
                cameraTexture = new WebCamTexture(devices[index].name, 640, 480, 20);
                cameraTexture.Play();
            }
            catch (Exception e) { Fail("Cannot start hand tracking: " + e.Message); yield break; }
            float deadline = Time.realtimeSinceStartup + 12;
            while (cameraTexture != null && (!cameraTexture.didUpdateThisFrame || cameraTexture.width <= 16) && Time.realtimeSinceStartup < deadline)
                yield return null;
            if (cameraTexture == null || cameraTexture.width <= 16)
            { Fail("Camera did not deliver images. Close other camera apps and retry."); yield break; }
            startup = null;
        }
        public void ClearCommands() { interpreter.ClearCommands(); }
        void Update()
        {
            if (!Selected) return;
            if (inference != null && inference.IsCompleted)
            {
                try
                {
                    Hands = inference.GetAwaiter().GetResult();
                    float now = Time.unscaledTime;
                    if (now - capturedAt < 0.4f)
                    {
                        interpreter.Process(Hands, now - lastResult, !game.IsPlaying && game.UI.IsGestureSetup);
                        if (interpreter.TwoHands) lastGoodHands = now;
                        if (game.IsPlaying && interpreter.Calibrated)
                        {
                            if (interpreter.Brake) game.Player.Brake();
                            if (interpreter.LaneStep != 0) game.Player.MoveLane(interpreter.LaneStep);
                        }
                    }
                    else { Hands = Array.Empty<TrackedHand>(); interpreter.Process(Hands, 0); }
                    lastResult = now;
                }
                catch (Exception e) { inference = null; Fail("Hand tracking failed: " + e.Message); return; }
                inference = null;
            }
            if (game.IsPlaying && (Time.unscaledTime - lastGoodHands > 0.8f || !string.IsNullOrEmpty(Error)))
            {
                game.TogglePause();
                game.UI.Toast("Tracking lost - recalibrate to resume", 4);
                return;
            }
            if (inference != null || engine == null || cameraTexture == null || cameraTexture.width <= 16 ||
                !cameraTexture.didUpdateThisFrame || Time.unscaledTime < nextFrame) return;
            try
            {
                if (previousRotation >= 0 && previousRotation != cameraTexture.videoRotationAngle && game.IsPlaying)
                {
                    game.TogglePause(); game.UI.Toast("Camera rotated - recalibrate to resume", 4); return;
                }
                PrepareFrame();
                capturedAt = Time.unscaledTime; nextFrame = capturedAt + 1f / 15f;
                timestamp = Math.Max(timestamp + 1, (long)(Time.realtimeSinceStartupAsDouble * 1000));
                int width = Preview.width, height = Preview.height;
                long frameTime = timestamp;
                inference = Task.Run(() => engine.Detect(pixels, width, height, frameTime));
            }
            catch (Exception e) { Fail("Camera frame failed: " + e.Message); }
        }
        void PrepareFrame()
        {
            int width = cameraTexture.width, height = cameraTexture.height;
            int rotation = cameraTexture.videoRotationAngle;
            if (previousRotation != rotation)
            { previousRotation = rotation; interpreter.ResetCalibration(); lastGoodHands = Time.unscaledTime; }
            bool swap = rotation == 90 || rotation == 270;
            float scale = Mathf.Min(1, 384f / Mathf.Max(width, height));
            int w = Mathf.Max(1, Mathf.RoundToInt((swap ? height : width) * scale));
            int h = Mathf.Max(1, Mathf.RoundToInt((swap ? width : height) * scale));
            if (source == null || source.Length != width * height) source = new Color32[width * height];
            cameraTexture.GetPixels32(source);
            if (Preview == null || Preview.width != w || Preview.height != h)
            {
                if (Preview != null) Destroy(Preview);
                Preview = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "Local front camera preview" };
                display = new Color32[w * h];
                if (pixels.IsCreated) pixels.Dispose();
                pixels = new NativeArray<byte>(w * h * 4, Allocator.Persistent);
            }
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var uv = CameraFrameTransform.SourceUV(new Vector2((x + 0.5f) / w, (y + 0.5f) / h),
                    rotation, cameraTexture.videoVerticallyMirrored, true);
                var color = source[Mathf.Clamp((int)(uv.y * height), 0, height - 1) * width + Mathf.Clamp((int)(uv.x * width), 0, width - 1)];
                display[y * w + x] = color;
                int i = ((h - y - 1) * w + x) * 4; // MediaPipe expects rows from the top.
                pixels[i] = color.r; pixels[i + 1] = color.g; pixels[i + 2] = color.b; pixels[i + 3] = 255;
            }
            Preview.SetPixels32(display); Preview.Apply(false, false);
        }
        void Fail(string message)
        {
            Error = message;
            StopCamera();
            if (game != null && game.IsPlaying) game.TogglePause();
        }
        public void StopCamera()
        {
            if (startup != null) { StopCoroutine(startup); startup = null; }
            if (cameraTexture != null) { cameraTexture.Stop(); Destroy(cameraTexture); cameraTexture = null; }
            // Never free native input memory or the detector while its worker is using it.
            if (inference != null) { try { inference.GetAwaiter().GetResult(); } catch { } inference = null; }
            engine?.Dispose(); engine = null;
            if (pixels.IsCreated) pixels.Dispose();
            if (Preview != null) { Destroy(Preview); Preview = null; }
            source = display = null; Hands = Array.Empty<TrackedHand>();
            requestingPermission = false;
            interpreter.ResetCalibration(); previousRotation = -1;
        }
        void OnApplicationPause(bool paused)
        {
            if (!paused || !Selected || requestingPermission) return;
            StopCamera(); Error = "Camera paused. Tap RETRY CAMERA to reconnect.";
            if (game != null && game.IsPlaying) game.TogglePause();
        }
        void OnDestroy() { StopCamera(); }
        void OnApplicationFocus(bool focused)
        {
            if (focused || !Selected || requestingPermission) return;
            StopCamera(); Error = "Camera paused. Tap RETRY CAMERA to reconnect.";
            if (game != null && game.IsPlaying) game.TogglePause();
        }
    }
}

#endif
