using UnityEngine;

namespace FinalFreedom
{
    // Built once per pooled traffic vehicle. Forward is +Z; wheel pivots rotate about X.
    public static class MotorcycleArt
    {
        public static Transform Create(Transform parent)
        {
            var bike = new GameObject("Motorcycle with rider").transform;
            bike.SetParent(parent, false);
            var frame = new GameObject("Motorcycle frame").transform;
            frame.SetParent(bike, false);
            var rubber = new Color(0.045f, 0.055f, 0.065f);
            var metal = new Color(0.52f, 0.59f, 0.63f);
            var paint = new Color(0.88f, 0.15f, 0.065f);
            var jacket = new Color(0.035f, 0.36f, 0.55f);
            var jeans = new Color(0.08f, 0.13f, 0.23f);
            var skin = new Color(0.75f, 0.49f, 0.31f);

            Wheel(bike, "Wheel_Rear", -0.86f, rubber, metal);
            Wheel(bike, "Wheel_Front", 0.86f, rubber, metal);
            Part(frame, "Engine", new Vector3(0, 0.58f, -0.02f), new Vector3(0.38f, 0.38f, 0.48f), metal);
            Part(frame, "Fuel tank", new Vector3(0, 0.97f, 0.16f), new Vector3(0.46f, 0.33f, 0.64f), paint);
            Part(frame, "Seat", new Vector3(0, 1.02f, -0.48f), new Vector3(0.42f, 0.14f, 0.64f), rubber);
            Part(frame, "Tail fairing", new Vector3(0, 0.89f, -0.78f), new Vector3(0.38f, 0.17f, 0.44f), paint);
            Part(frame, "Front mudguard", new Vector3(0, 0.75f, 0.88f), new Vector3(0.27f, 0.09f, 0.55f), paint);
            Part(frame, "Headlight housing", new Vector3(0, 1.14f, 0.78f), new Vector3(0.35f, 0.27f, 0.22f), paint);
            Part(frame, "Headlight", new Vector3(0, 1.14f, 0.9f), new Vector3(0.27f, 0.18f, 0.035f), new Color(1, 0.95f, 0.7f));
            Part(frame, "Tail lamp", new Vector3(0, 0.94f, -1.01f), new Vector3(0.25f, 0.09f, 0.035f), Color.red);
            Part(frame, "Rear plate", new Vector3(0, 0.78f, -1.01f), new Vector3(0.23f, 0.15f, 0.025f), Color.white);
            Rod(frame, "Exhaust", new Vector3(0.3f, 0.48f, -0.15f), new Vector3(0.3f, 0.48f, -1.04f), 0.12f, metal);
            Rod(frame, "Handlebar", new Vector3(-0.46f, 1.26f, 0.61f), new Vector3(0.46f, 1.26f, 0.61f), 0.065f, metal);
            for (int s = -1; s <= 1; s += 2)
            {
                Rod(frame, "Front fork", new Vector3(s * 0.15f, 0.36f, 0.86f), new Vector3(s * 0.15f, 1.26f, 0.57f), 0.065f, metal);
                Rod(frame, "Swingarm", new Vector3(s * 0.15f, 0.36f, -0.86f), new Vector3(s * 0.15f, 0.6f, 0.08f), 0.085f, metal);
                Rod(frame, "Frame rail", new Vector3(s * 0.18f, 0.57f, 0.05f), new Vector3(s * 0.18f, 0.97f, -0.64f), 0.075f, rubber);
                Rod(frame, "Mirror stem", new Vector3(s * 0.4f, 1.26f, 0.61f), new Vector3(s * 0.53f, 1.48f, 0.66f), 0.035f, metal);
                Part(frame, "Mirror", new Vector3(s * 0.53f, 1.48f, 0.66f), new Vector3(0.17f, 0.11f, 0.05f), rubber);
            }

            var rider = new GameObject("Rider").transform;
            rider.SetParent(bike, false);
            Part(rider, "Hips", new Vector3(0, 1.14f, -0.36f), new Vector3(0.44f, 0.22f, 0.34f), jeans);
            var torso = Part(rider, "Jacket", new Vector3(0, 1.5f, -0.19f), new Vector3(0.51f, 0.62f, 0.3f), jacket);
            torso.localRotation = Quaternion.Euler(19, 0, 0);
            Part(rider, "Neck", new Vector3(0, 1.83f, -0.08f), new Vector3(0.16f, 0.17f, 0.16f), skin);
            Part(rider, "Helmet", new Vector3(0, 2.02f, -0.04f), new Vector3(0.45f, 0.49f, 0.45f), new Color(0.96f, 0.79f, 0.18f), PrimitiveType.Sphere);
            Part(rider, "Helmet visor", new Vector3(0, 2.04f, 0.158f), new Vector3(0.35f, 0.16f, 0.075f), new Color(0.035f, 0.09f, 0.14f));
            for (int s = -1; s <= 1; s += 2)
            {
                var elbow = new Vector3(s * 0.36f, 1.4f, 0.18f);
                var hand = new Vector3(s * 0.42f, 1.28f, 0.61f);
                Rod(rider, "Upper arm", new Vector3(s * 0.24f, 1.7f, -0.1f), elbow, 0.18f, jacket);
                Rod(rider, "Forearm", elbow, hand, 0.15f, jacket);
                Part(rider, "Glove", hand, new Vector3(0.14f, 0.13f, 0.18f), rubber);
                var knee = new Vector3(s * 0.31f, 0.87f, 0.17f);
                Rod(rider, "Thigh", new Vector3(s * 0.19f, 1.16f, -0.35f), knee, 0.21f, jeans);
                Rod(rider, "Shin", knee, new Vector3(s * 0.31f, 0.55f, -0.08f), 0.17f, jeans);
                Part(rider, "Boot", new Vector3(s * 0.31f, 0.5f, 0.015f), new Vector3(0.2f, 0.16f, 0.34f), rubber);
            }
            return bike;
        }

        static Transform Part(Transform parent, string name, Vector3 position, Vector3 size, Color color,
            PrimitiveType shape = PrimitiveType.Cube) => ProceduralArt.Part(name, parent, position, size, color, shape).transform;

        static void Rod(Transform parent, string name, Vector3 from, Vector3 to, float width, Color color)
        {
            var rod = Part(parent, name, (from + to) * 0.5f, new Vector3(width, (to - from).magnitude, width), color);
            rod.localRotation = Quaternion.FromToRotation(Vector3.up, to - from);
        }

        static void Wheel(Transform parent, string name, float z, Color rubber, Color metal)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = new Vector3(0, 0.36f, z);
            var tire = Part(pivot, "Tire", Vector3.zero, new Vector3(0.72f, 0.105f, 0.72f), rubber, PrimitiveType.Cylinder);
            tire.localRotation = Quaternion.Euler(0, 0, 90);
            for (int s = -1; s <= 1; s += 2)
            {
                var hub = Part(pivot, "Wheel hub", new Vector3(s * 0.108f, 0, 0), new Vector3(0.48f, 0.012f, 0.48f), metal, PrimitiveType.Cylinder);
                hub.localRotation = Quaternion.Euler(0, 0, 90);
                Part(pivot, "Spoke", new Vector3(s * 0.125f, 0, 0), new Vector3(0.015f, 0.055f, 0.45f), rubber);
                Part(pivot, "Spoke", new Vector3(s * 0.125f, 0, 0), new Vector3(0.015f, 0.45f, 0.055f), rubber);
            }
        }
    }
}
