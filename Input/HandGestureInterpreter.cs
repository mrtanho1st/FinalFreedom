using UnityEngine;

namespace FinalFreedom
{
    // WebGL receives this nested type through JsonUtility. Without Serializable,
    // the browser can detect hands while Unity silently discards the hands array.
    [System.Serializable]
    public sealed class TrackedHand
    {
        // Upright, mirrored camera coordinates: origin at bottom left, X corrected for aspect.
        public Vector2[] points = new Vector2[21];
        public Vector2 Palm => (points[0] + points[5] + points[9] + points[13] + points[17]) / 5f;
        public bool ThumbsUp
        {
            get
            {
                float palmSize = Vector2.Distance(points[0], points[9]);
                if (palmSize < 0.02f) return false;
                // Thumb straight and pointing upwards in the normalized selfie image.
                Vector2 thumb = points[4] - points[2];
                if (thumb.y < palmSize * 0.65f || thumb.normalized.y < 0.75f ||
                    points[4].y < Palm.y + palmSize * 0.6f ||
                    Vector2.Dot((points[3] - points[2]).normalized, (points[4] - points[3]).normalized) < 0.7f)
                    return false;
                // All other fingers folded; an open palm or pointing index is not a start command.
                for (int tip = 8; tip <= 20; tip += 4)
                    if (Vector2.Distance(points[tip], points[0]) >= Vector2.Distance(points[tip - 2], points[0]) * 1.05f)
                        return false;
                return true;
            }
        }
        public bool Open
        {
            get
            {
                int extended = 0;
                for (int tip = 8; tip <= 20; tip += 4)
                {
                    var a = points[tip - 2] - points[tip - 3];
                    var b = points[tip] - points[tip - 2];
                    if (Vector2.Dot(a.normalized, b.normalized) > 0.7f &&
                        Vector2.Distance(points[tip], points[0]) > Vector2.Distance(points[tip - 2], points[0]) * 1.15f)
                        extended++;
                }
                return extended >= 3;
            }
        }
    }

    // Landmark AI supplies hands; this deterministic interpreter supplies debounced game commands.
    public sealed class HandGestureInterpreter
    {
        public bool Calibrated { get; private set; }
        public float CalibrationProgress => Mathf.Clamp01(stableSeconds / 0.8f);
        public float WheelAngle { get; private set; }
        public bool Nitro { get; private set; }
        public bool Brake { get; private set; }
        public int LaneStep { get; private set; }
        public bool TwoHands { get; private set; }
        public bool StartRequested { get; private set; }
        public float StartProgress => Mathf.Clamp01(thumbSeconds / 0.7f);
        float thumbSeconds;
        bool startArmed = true;
        float stableSeconds, neutralGap, neutralAngle, turnSeconds, closeSeconds;
        Vector2 lastLeft, lastRight;
        int pendingTurn;
        bool armed = true, hasPrevious;

        public void ResetCalibration()
        {
            Calibrated = false; stableSeconds = 0; hasPrevious = false;
            ClearCommands();
        }
        public void ClearCommands()
        {
            Nitro = Brake = TwoHands = false; LaneStep = 0;
            StartRequested = false; thumbSeconds = 0; startArmed = true;
            turnSeconds = closeSeconds = 0; pendingTurn = 0; armed = false; WheelAngle = 0;
        }
        public bool ConsumeStartRequest()
        {
            bool requested = StartRequested; StartRequested = false; return requested;
        }
        public void Process(TrackedHand[] hands, float dt, bool detectStart = false)
        {
            LaneStep = 0; Brake = false;
            dt = Mathf.Clamp(dt, 0, 0.15f); // A delayed frame cannot satisfy a dwell by itself.
            TwoHands = hands != null && hands.Length == 2;
            if (!TwoHands)
            {
                ClearCommands(); stableSeconds = 0; hasPrevious = false;
                return;
            }
            var left = hands[0].Palm; var right = hands[1].Palm;
            if (left.x > right.x) { var swap = left; left = right; right = swap; }
            Vector2 span = right - left;
            float gap = span.magnitude;
            if (gap < 0.10f) { ClearCommands(); stableSeconds = 0; return; }
            float angle = Mathf.Atan2(span.y, span.x) * Mathf.Rad2Deg;
            bool open = hands[0].Open || hands[1].Open;
            if (!Calibrated)
            {
                bool steady = hasPrevious && Vector2.Distance(left, lastLeft) < 0.035f &&
                    Vector2.Distance(right, lastRight) < 0.035f && Mathf.Abs(angle) < 12 && gap > 0.20f && !open;
                stableSeconds = steady ? stableSeconds + dt : 0;
                lastLeft = left; lastRight = right; hasPrevious = true;
                if (stableSeconds >= 0.8f) { neutralGap = gap; neutralAngle = angle; Calibrated = true; }
                return;
            }
            bool thumbUp = detectStart && (hands[0].ThumbsUp || hands[1].ThumbsUp);
            if (!thumbUp) { thumbSeconds = 0; StartRequested = false; startArmed = true; }
            else if (startArmed)
            {
                thumbSeconds += dt;
                if (thumbSeconds >= 0.7f) { StartRequested = true; startArmed = false; }
            }
            WheelAngle = Mathf.Lerp(WheelAngle, Mathf.DeltaAngle(neutralAngle, angle), 1 - Mathf.Exp(-12 * dt));
            Brake = open;
            if (Brake)
            {
                Nitro = false; closeSeconds = turnSeconds = 0; pendingTurn = 0;
                return;
            }
            float ratio = gap / neutralGap;
            if (ratio > 0.85f) { Nitro = false; closeSeconds = 0; }
            else if (ratio < 0.70f)
            {
                closeSeconds += dt;
                if (closeSeconds >= 0.2f) Nitro = true;
            }
            else closeSeconds = 0;

            if (Mathf.Abs(WheelAngle) < 8) { armed = true; pendingTurn = 0; turnSeconds = 0; }
            int turn = WheelAngle > 18 ? -1 : WheelAngle < -18 ? 1 : 0;
            if (turn == 0 || !armed) { turnSeconds = 0; pendingTurn = 0; return; }
            if (pendingTurn != turn) { pendingTurn = turn; turnSeconds = 0; }
            turnSeconds += dt;
            if (turnSeconds >= 0.16f) { LaneStep = turn; armed = false; turnSeconds = 0; }
        }
    }
}
