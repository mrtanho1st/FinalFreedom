using UnityEngine;

namespace FinalFreedom
{
    public static class CameraFrameTransform
    {
        // Inverse of Unity's documented webcam display correction, followed by selfie mirroring.
        public static Vector2 SourceUV(Vector2 display, int clockwiseRotation, bool verticalFlip, bool mirror)
        {
            float u = mirror ? 1 - display.x : display.x, v = display.y;
            switch ((clockwiseRotation % 360 + 360) % 360)
            {
                case 90: (u, v) = (1 - v, u); break;
                case 180: (u, v) = (1 - u, 1 - v); break;
                case 270: (u, v) = (v, 1 - u); break;
            }
            if (verticalFlip) v = 1 - v;
            return new Vector2(u, v);
        }
    }
}
