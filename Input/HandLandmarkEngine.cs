using System;
using Mediapipe.Tasks.Core;
using Mediapipe.Tasks.Vision.Core;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Unity.Collections;
using UnityEngine;

namespace FinalFreedom
{
    public sealed class HandLandmarkEngine : IDisposable
    {
        readonly HandLandmarker detector;
        HandLandmarkerResult result = HandLandmarkerResult.Alloc(2);
        public HandLandmarkEngine(byte[] model)
        {
            detector = HandLandmarker.CreateFromOptions(new HandLandmarkerOptions(
                new BaseOptions(BaseOptions.Delegate.CPU, modelAssetBuffer: model), RunningMode.VIDEO,
                numHands: 2, minHandDetectionConfidence: 0.65f, minHandPresenceConfidence: 0.65f,
                minTrackingConfidence: 0.6f));
        }
        // Called by a single worker. The caller owns the buffer until inference has finished.
        public TrackedHand[] Detect(NativeArray<byte> rgba, int width, int height, long milliseconds)
        {
            using var image = new Mediapipe.Image(Mediapipe.ImageFormat.Types.Format.Srgba, width, height, width * 4, rgba);
            if (!detector.TryDetectForVideo(image, milliseconds, null, ref result) || result.handLandmarks == null)
                return Array.Empty<TrackedHand>();
            var hands = new TrackedHand[result.handLandmarks.Count];
            float aspect = (float)width / height;
            for (int h = 0; h < hands.Length; h++)
            {
                var landmarks = result.handLandmarks[h].landmarks;
                if (landmarks.Count != 21) return Array.Empty<TrackedHand>();
                hands[h] = new TrackedHand();
                for (int i = 0; i < 21; i++)
                {
                    var point = landmarks[i];
                    if (float.IsNaN(point.x) || float.IsNaN(point.y) || float.IsInfinity(point.x) || float.IsInfinity(point.y))
                        return Array.Empty<TrackedHand>();
                    hands[h].points[i] = new Vector2(point.x * aspect, 1 - point.y);
                }
            }
            return hands;
        }
        public void Dispose() => ((IDisposable)detector).Dispose();
    }
}
