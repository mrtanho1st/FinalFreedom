using UnityEngine;

namespace FinalFreedom
{
    public sealed class TrackGenerator : MonoBehaviour
    {
        const float Length = 40f;
        const int Count = 12;

        readonly Transform[] chunks = new Transform[Count];

        GameManager game;
        Transform roadColliderRoot;

        public void Initialize(GameManager manager)
        {
            game = manager;

            // Một collider liền mạch cho toàn bộ mặt đường.
            // Mặt trên ở y = 0, trùng với mặt trên của hình ảnh đường.
            var collisionRoad = new GameObject("Continuous road collider");
            collisionRoad.layer = GameLayers.Road;
            collisionRoad.transform.SetParent(game.World, false);
            roadColliderRoot = collisionRoad.transform;

            var floor = collisionRoad.AddComponent<BoxCollider>();
            floor.size = new Vector3(20f, 0.4f, 4000f);
            floor.center = new Vector3(0f, -0.2f, 0f);
            floor.sharedMaterial = ProceduralArt.VehiclePhysics;

            var oldState = Random.state;
            Random.InitState(game.Config.randomSeed);

            for (int i = 0; i < Count; i++)
            {
                var root = new GameObject("City block " + i);
                root.transform.SetParent(game.World, false);
                root.layer = GameLayers.Road;
                chunks[i] = root.transform;
                var street = new GameObject("Street geometry").transform;
                street.SetParent(root.transform, false);

                // Không thêm BoxCollider vào từng City block:
                // mặt trước/sau của chúng từng chặn xe ở mép nối.
                for (int j = 0; j < 10; j++)
                {
                    float z = -18 + j * 4;

                    ProceduralArt.Part(
                        "Road", street,
                        new Vector3(0, -0.15f, z),
                        new Vector3(12.5f, 0.3f, 4),
                        ProceduralArt.Asphalt);

                    for (int stripe = -1; stripe <= 1; stripe++)
                    {
                        float x = stripe * 3;
                        ProceduralArt.Part(
                            "Lane marking", street,
                            new Vector3(x, 0.014f, z),
                            new Vector3(
                                stripe == 0 ? 0.12f : 0.065f,
                                0.02f,
                                stripe == 0 ? 4f : 2.4f),
                            stripe == 0
                                ? ProceduralArt.Gold
                                : new Color(0.72f, 0.79f, 0.75f));
                    }

                    for (int sign = -1; sign <= 1; sign += 2)
                    {
                        ProceduralArt.Part(
                            "Sidewalk", street,
                            new Vector3(sign * 7.3f, -0.02f, z),
                            new Vector3(2.1f, 0.35f, 4),
                            new Color(0.43f, 0.55f, 0.54f));

                        ProceduralArt.Part(
                            "Edge", street,
                            new Vector3(sign * 6.2f, 0.02f, z),
                            new Vector3(0.16f, 0.025f, 4),
                            ProceduralArt.Gold);
                    }
                }

                for (int side = -1; side <= 1; side += 2)
                {
                    for (int b = 0; b < 4; b++)
                    {
                        float z = -15 + b * 10;
                        var prefab = CityPrefabLibrary.Pick(CityPrefabLibrary.Instance.buildings);
                        var building = PrefabArt.Instantiate(prefab, root.transform);
                        building.localRotation = Quaternion.Euler(0, side < 0 ? 90 : -90, 0);
                        PrefabArt.Fit(building, new Vector3(7f, 19f, 8f), new Vector3(side * 13f, 0f, z));

                        ProceduralArt.Part(
                            "Streetlamp", street,
                            new Vector3(side * 7.8f, 2, z + 3),
                            new Vector3(0.12f, 4, 0.12f),
                            ProceduralArt.Ink);

                        ProceduralArt.Part(
                            "Lamp head", street,
                            new Vector3(side * 7.4f, 4, z + 3),
                            new Vector3(1, 0.15f, 0.5f),
                            ProceduralArt.Gold);
                    }

                    ProceduralArt.Part(
                        "Planter", street,
                        new Vector3(side * 7.5f, 0.5f, 0),
                        new Vector3(1, 1, 2),
                        new Color(0.19f, 0.38f, 0.31f));
                }

                // Imported prefab meshes may not be CPU-readable in a player build.
                // Batch only the generated street; keep textured buildings as prefab instances.
                ProceduralArt.MergeStatic(street);
            }

            Random.state = oldState;
            ResetWorld();
        }

        public void ResetWorld()
        {
            if (roadColliderRoot != null)
                roadColliderRoot.position = Vector3.zero;

            for (int i = 0; i < Count; i++)
                chunks[i].position = new Vector3(0, 0, (i - 2) * Length);
        }

        void Update()
        {
            if (!game.IsPlaying) return;

            float z = game.Player.transform.position.z;
            foreach (var chunk in chunks)
            {
                if (chunk.position.z + Length * 0.5f < z - 60f)
                    chunk.position += Vector3.forward * (Count * Length);
            }
        }

        public void Shift(float shift)
        {
            // GameManager vừa dịch mọi con của World lùi đi "shift".
            // Đưa collider dài trở lại tâm để nó luôn bao phủ khu vực chơi.
            if (roadColliderRoot != null)
                roadColliderRoot.position += Vector3.forward * shift;
        }
    }
}