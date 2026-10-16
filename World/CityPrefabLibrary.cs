using System;
using UnityEngine;

namespace FinalFreedom
{
    // Direct asset references keep the original pack in place and include its dependencies in builds.
    [CreateAssetMenu(menuName = "FinalFreedom/City Prefab Library")]
    public sealed class CityPrefabLibrary : ScriptableObject
    {
        public GameObject[] playerCars;
        public GameObject police;
        public GameObject smallCar;
        public GameObject[] sedans;
        public GameObject[] trucks;
        public GameObject[] buses;
        public GameObject[] containers;
        public GameObject[] buildings;
        public GameObject sidewalk;
        public GameObject groundTile;
        public GameObject constructionBarrier;
        public GameObject policeRoadblock;
        public GameObject roadblockCone;

        static CityPrefabLibrary instance;
        public static CityPrefabLibrary Instance
        {
            get
            {
                if (instance == null) instance = Resources.Load<CityPrefabLibrary>("CityPrefabLibrary");
                if (instance == null) throw new InvalidOperationException("Missing Resources/CityPrefabLibrary.asset.");
                return instance;
            }
        }

        public GameObject Vehicle(VehicleKind kind)
        {
            switch (kind)
            {
                case VehicleKind.Player: return playerCars[0];
                case VehicleKind.Police: return police;
                // SimplePoly City has no motorcycle; keep the light traffic slot as a compact car.
                case VehicleKind.Motorcycle: return smallCar;
                case VehicleKind.Truck: return Pick(trucks);
                case VehicleKind.Bus: return Pick(buses);
                case VehicleKind.Container: return Pick(containers);
                default: return Pick(sedans);
            }
        }

        public static GameObject Pick(GameObject[] prefabs)
        {
            if (prefabs == null || prefabs.Length == 0)
                throw new InvalidOperationException("City prefab collection is empty.");
            return prefabs[UnityEngine.Random.Range(0, prefabs.Length)];
        }
    }
}
