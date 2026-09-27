using System.Collections.Generic;
using UnityEngine;

namespace FinalFreedom
{
    public static class VehicleFactory
    {
        public static GameObject Create(VehicleKind kind)
        {
            float mass = 2200, hp = 70, width = 1.9f, length = 3.9f;
            Color paint = new Color(0.9f, 0.32f, 0.25f);
            switch (kind)
            {
                case VehicleKind.Motorcycle: mass = 1200; hp = 30; width = 0.8f; length = 2.4f; paint = ProceduralArt.Gold; break;
                case VehicleKind.Truck: mass = 3800; hp = 100; width = 2.2f; length = 6f; paint = new Color(0.18f, 0.6f, 0.55f); break;
                case VehicleKind.Bus: mass = 4000; hp = 130; width = 2.3f; length = 7.6f; paint = new Color(0.93f, 0.63f, 0.15f); break;
                case VehicleKind.Container: mass = 6000; hp = 150; width = 2.35f; length = 10f; paint = new Color(0.62f, 0.36f, 0.57f); break;
                case VehicleKind.Player: hp = 100; paint = ProceduralArt.Cyan; break;
                case VehicleKind.Police: hp = 100; paint = new Color(0.84f, 0.9f, 0.92f); break;
            }
            var root = new GameObject(kind.ToString());
            root.layer = kind == VehicleKind.Player ? GameLayers.Player : kind == VehicleKind.Police ? GameLayers.Police : GameLayers.Traffic;
            var body = root.AddComponent<Rigidbody>(); body.mass = mass;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.linearDamping = 0.08f; body.angularDamping = 1.2f;
            body.centerOfMass = new Vector3(0, -0.25f, 0);
            var box = root.AddComponent<BoxCollider>(); box.size = new Vector3(width, 1.2f, length); box.center = new Vector3(0, 0.1f, 0);
            box.sharedMaterial = ProceduralArt.VehiclePhysics;
            var motor = root.AddComponent<VehicleMotor>(); motor.Length = length;
            var model = new GameObject("MODEL - replace with your artwork").transform; model.SetParent(root.transform, false);
            var panels = new List<Renderer>(); var wheels = new List<Transform>(); var brakeLights = new List<Renderer>();
            panels.Add(Part("Body", model, new Vector3(0, 0.12f, 0), new Vector3(width, 0.52f, length), paint));
            Color glass = new Color(0.12f, 0.27f, 0.34f);
            if (kind == VehicleKind.Motorcycle)
            {
                Part("Seat", model, new Vector3(0, 0.5f, -0.25f), new Vector3(0.5f, 0.25f, 0.9f), ProceduralArt.Ink);
                Part("Rider", model, new Vector3(0, 0.91f, -0.2f), new Vector3(0.55f, 0.75f, 0.45f), glass);
                ProceduralArt.Part("Helmet", model, new Vector3(0, 1.47f, 0), Vector3.one * 0.52f, paint, PrimitiveType.Sphere);
            }
            else if (kind == VehicleKind.Truck || kind == VehicleKind.Container)
            {
                float cabZ = length / 2 - 1;
                panels.Add(Part("Cab", model, new Vector3(0, 0.95f, cabZ), new Vector3(width * 0.94f, 1.25f, 1.9f), paint));
                Part("Windshield", model, new Vector3(0, 1.12f, cabZ + 0.96f), new Vector3(width * 0.8f, 0.52f, 0.03f), glass);
                Part("Cargo", model, new Vector3(0, 1.3f, -1), new Vector3(width, 1.8f, length - 2.2f), kind == VehicleKind.Container ? paint * 0.8f : new Color(0.76f, 0.78f, 0.72f));
                for (int i = 0; i < 4; i++) Part("Cargo rail", model, new Vector3(0, 2.23f, -length / 2 + 0.5f + i * (length - 2.5f) / 4), new Vector3(width + 0.04f, 0.06f, 0.08f), glass);
            }
            else if (kind == VehicleKind.Bus)
            {
                panels.Add(Part("Bus cabin", model, new Vector3(0, 1f, 0), new Vector3(width, 1.4f, length - 0.1f), paint));
                Part("Window band", model, new Vector3(0, 1.34f, 0.02f), new Vector3(width + 0.03f, 0.5f, length - 0.3f), glass);
                for (int i = 0; i < 7; i++) Part("Window pillar", model, new Vector3(0, 1.34f, -length / 2 + 0.6f + i), new Vector3(width + 0.06f, 0.54f, 0.07f), paint);
            }
            else
            {
                Part("Glass cabin", model, new Vector3(0, 0.67f, -0.15f), new Vector3(width * 0.83f, 0.68f, length * 0.48f), glass);
                panels.Add(Part("Roof", model, new Vector3(0, 1.03f, -0.22f), new Vector3(width * 0.87f, 0.12f, length * 0.36f), paint));
                Part("Racing stripe", model, new Vector3(0, 0.395f, 1.3f), new Vector3(0.28f, 0.018f, 1.05f), ProceduralArt.Ink);
                Part("Spoiler", model, new Vector3(0, 0.68f, -length / 2 + 0.25f), new Vector3(width + 0.12f, 0.12f, 0.3f), ProceduralArt.Ink);
            }
            int axles = length > 7 ? 3 : 2;
            for (int axle = 0; axle < axles; axle++)
            {
                float z = Mathf.Lerp(-length * 0.33f, length * 0.32f, axle / (float)(axles - 1));
                int count = kind == VehicleKind.Motorcycle ? 1 : 2;
                for (int side = 0; side < count; side++)
                {
                    float x = count == 1 ? 0 : (side == 0 ? -1 : 1) * width * 0.48f;
                    var wheel = ProceduralArt.Part("Wheel", model, new Vector3(x, -0.2f, z), new Vector3(0.65f, 0.16f, 0.65f), ProceduralArt.Ink, PrimitiveType.Cylinder);
                    wheel.transform.localRotation = Quaternion.Euler(0, 0, 90); wheels.Add(wheel.transform);
                }
            }
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * width * 0.33f;
                Part("Headlight", model, new Vector3(x, 0.2f, length * 0.5f + 0.02f), new Vector3(width * 0.22f, 0.15f, 0.04f), new Color(1, 0.93f, 0.63f));
                brakeLights.Add(Part("Brake light", model, new Vector3(x, 0.18f, -length * 0.5f - 0.02f), new Vector3(width * 0.22f, 0.16f, 0.04f), Color.red * 0.4f));
            }
            var visual = root.AddComponent<VehicleVisual>();
            visual.Configure(model, panels.ToArray(), wheels.ToArray(), brakeLights.ToArray(), kind);
            var damage = root.AddComponent<VehicleDamage>(); damage.Configure(hp, kind == VehicleKind.Player);
            if (kind != VehicleKind.Player) root.AddComponent<WorldHealthBar>().Initialize(damage, length > 5 ? 3f : 2.2f);
            return root;
        }
        static Renderer Part(string name, Transform parent, Vector3 p, Vector3 s, Color c) => ProceduralArt.Part(name, parent, p, s, c).GetComponent<Renderer>();
    }
}
