using System.Collections.Generic;
using UnityEngine;

namespace FinalFreedom
{
    public static class VehicleFactory
    {
        public static GameObject Create(VehicleKind kind)
        {
            float mass = 2200, hp = 70, width = 1.9f, length = 3.9f;
            switch (kind)
            {
                case VehicleKind.Motorcycle: mass = 1200; hp = 30; width = 1.35f; length = 2.8f; break;
                case VehicleKind.Truck: mass = 3800; hp = 100; width = 2.2f; length = 6f; break;
                case VehicleKind.Bus: mass = 4000; hp = 130; width = 2.3f; length = 7.6f; break;
                case VehicleKind.Container: mass = 6000; hp = 150; width = 2.35f; length = 10f; break;
                case VehicleKind.Player: case VehicleKind.Police: hp = 100; break;
            }
            var prefab = kind == VehicleKind.Player ? SaveService.CurrentVehicle.prefab : CityPrefabLibrary.Instance.Vehicle(kind);
            var root = new GameObject(kind.ToString());
            root.layer = kind == VehicleKind.Player ? GameLayers.Player : kind == VehicleKind.Police ? GameLayers.Police : GameLayers.Traffic;
            var body = root.AddComponent<Rigidbody>(); body.mass = mass;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.linearDamping = 0.08f; body.angularDamping = 1.2f;
            root.AddComponent<BoxCollider>();
            root.AddComponent<VehicleMotor>();
            root.AddComponent<VehicleVisual>();
            var bounds = BuildModel(root, prefab, kind, kind == VehicleKind.Player
                ? SaveService.CurrentVehicle.maximumSize : new Vector3(width, 3.4f, length));
            var damage = root.AddComponent<VehicleDamage>();
            damage.Configure(kind == VehicleKind.Player ? SaveService.CurrentVehicle.health : hp, kind == VehicleKind.Player);
            if (kind != VehicleKind.Player) root.AddComponent<WorldHealthBar>().Initialize(damage, bounds.max.y + 0.6f);
            return root;
        }

        public static void ApplyPlayerVehicle(GameObject root, PlayerVehicle vehicle)
        {
            var old = root.transform.Find("Vehicle model");
            if (old != null)
            {
                old.name = "Retired vehicle model";
                old.gameObject.SetActive(false);
                Object.Destroy(old.gameObject);
            }
            BuildModel(root, vehicle.prefab, VehicleKind.Player, vehicle.maximumSize);
        }

        static Bounds BuildModel(GameObject root, GameObject prefab, VehicleKind kind, Vector3 maximumSize)
        {
            // Unit-scale wrapper: effect positions remain in game units.
            var model = new GameObject("Vehicle model").transform;
            model.SetParent(root.transform, false);
            var artwork = kind == VehicleKind.Motorcycle ? MotorcycleArt.Create(model) : PrefabArt.Instantiate(prefab, model);
            var bounds = PrefabArt.Fit(artwork, maximumSize, new Vector3(0, -0.5f, 0));
            var box = root.GetComponent<BoxCollider>();
            box.size = bounds.size; box.center = bounds.center;
            box.sharedMaterial = ProceduralArt.VehiclePhysics;
            var body = root.GetComponent<Rigidbody>();
            body.ResetInertiaTensor();
            body.centerOfMass = new Vector3(0, -0.25f, 0);
            root.GetComponent<VehicleMotor>().Length = bounds.size.z;

            var wheels = new List<Transform>();
            foreach (var part in artwork.GetComponentsInChildren<Transform>())
                if (part.name.StartsWith("Wheel_", System.StringComparison.Ordinal)) wheels.Add(part);
            var brakeLights = new List<Renderer>();
            if (kind == VehicleKind.Motorcycle)
                brakeLights.Add(artwork.Find("Motorcycle frame/Tail lamp").GetComponent<Renderer>());
            else for (int side = -1; side <= 1; side += 2)
            {
                var light = ProceduralArt.Part("Brake glow", model,
                    new Vector3(side * bounds.size.x * 0.33f, bounds.min.y + bounds.size.y * 0.35f, bounds.min.z - 0.015f),
                    new Vector3(bounds.size.x * 0.16f, 0.12f, 0.03f), Color.red * 0.4f);
                brakeLights.Add(light.GetComponent<Renderer>());
            }
            var visual = root.GetComponent<VehicleVisual>();
            visual.Configure(model, artwork.GetComponentsInChildren<Renderer>(), wheels.ToArray(), brakeLights.ToArray(), kind, bounds);
            return bounds;
        }
    }
}
