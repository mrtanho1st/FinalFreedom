using System;
using UnityEngine;

namespace FinalFreedom
{
    [Serializable]
    public sealed class PlayerVehicle
    {
        public string id;
        public string displayName;
        public GameObject prefab;
        public Vector3 maximumSize = new Vector3(1.9f, 2.3f, 4.2f);
        public float health = 100;
        public float speedBonus;
    }

    // Serialized references work in builds without AssetDatabase or copies of the prefab assets.
    public sealed class VehicleCatalog : ScriptableObject
    {
        public PlayerVehicle[] vehicles = Array.Empty<PlayerVehicle>();
        static VehicleCatalog instance;
        public static VehicleCatalog Instance => instance != null ? instance :
            instance = Resources.Load<VehicleCatalog>("VehicleCatalog") ?? throw new InvalidOperationException("Missing VehicleCatalog resource.");
        public int IndexOf(string id) => Array.FindIndex(vehicles, vehicle => vehicle.id == id);

        public static Vector3 DefaultSize(string name)
        {
            if (name.StartsWith("Vehicle_", StringComparison.OrdinalIgnoreCase)) name = name.Substring(8);
            // Class-specific envelopes, with a single uniform scale factor applied by PrefabArt.Fit.
            if (name.StartsWith("Container", StringComparison.OrdinalIgnoreCase)) return new Vector3(2.4f, 3.8f, 10f);
            if (name.StartsWith("Bus", StringComparison.OrdinalIgnoreCase)) return new Vector3(2.35f, 3.7f, 8f);
            if (name.StartsWith("Truck", StringComparison.OrdinalIgnoreCase)) return new Vector3(2.3f, 3.6f, 6.5f);
            if (name.StartsWith("Pick", StringComparison.OrdinalIgnoreCase)) return new Vector3(2.1f, 2.8f, 5.5f);
            if (name.StartsWith("Ambulance", StringComparison.OrdinalIgnoreCase)) return new Vector3(2.15f, 3f, 5.4f);
            if (name.StartsWith("SUV", StringComparison.OrdinalIgnoreCase)) return new Vector3(2.05f, 2.7f, 4.9f);
            return new Vector3(1.9f, 2.3f, 4.2f);
        }
    }
}
