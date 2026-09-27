using UnityEngine;

namespace FinalFreedom
{
    public sealed class ObstacleSpawner : MonoBehaviour
    {
        ObjectPooler[] pools;
        GameManager game;
        float nextZ;

        public void Initialize(GameManager manager)
        {
            game = manager;
            // 0 = Construction barrier
            // 1 = Pothole
            // 2 = Police roadblock
            pools = new ObjectPooler[3];

            for (int i = 0; i < pools.Length; i++)
            {
                int type = i;
                pools[i] = new ObjectPooler(
                    game.World,
                    () => Create(type),
                    3,
                    8
                );
            }
        }

        GameObject Create(int type)
        {
            string[] names =
            {
                "Construction barrier",
                "Pothole",
                "Police roadblock"
            };

            var root = new GameObject(names[type]);
            root.layer = GameLayers.Obstacle;

            var obstacle = root.AddComponent<RoadObstacle>();
            obstacle.IsPothole = type == 1;

            var box = root.AddComponent<BoxCollider>();
            box.sharedMaterial = ProceduralArt.VehiclePhysics;

            // Pothole
            if (type == 1)
            {
                box.isTrigger = true;
                box.size = new Vector3(2.3f, 0.65f, 2.5f);
                box.center = new Vector3(0, 0.25f, 0);

                ProceduralArt.Part(
                    "Broken asphalt",
                    root.transform,
                    new Vector3(0, 0.025f, 0),
                    new Vector3(2.4f, 0.035f, 2.8f),
                    new Color(0.04f, 0.07f, 0.08f),
                    PrimitiveType.Cylinder
                );
            }
            // Construction barrier / Police roadblock
            else
            {
                float width = 2.55f;
                float length = 0.65f;

                box.size = new Vector3(width, 1.1f, length);
                box.center = new Vector3(0, 0.55f, 0);

                Color baseColor =
                    type == 2
                        ? new Color(0.19f, 0.32f, 0.45f)
                        : new Color(1f, 0.35f, 0.08f);

                ProceduralArt.Part(
                    "Barrier body",
                    root.transform,
                    box.center,
                    box.size,
                    baseColor
                );

                for (int j = 0; j < 3; j++)
                {
                    ProceduralArt.Part(
                        "Warning panel",
                        root.transform,
                        new Vector3(
                            (j - 1) * width / 3,
                            0.65f,
                            -length / 2 - 0.01f
                        ),
                        new Vector3(
                            width / 6,
                            0.35f,
                            0.04f
                        ),
                        ProceduralArt.Gold
                    );
                }

                // Police roadblock
                if (type == 2)
                {
                    ProceduralArt.Part(
                        "Police strobe",
                        root.transform,
                        new Vector3(0, 1.2f, 0),
                        new Vector3(0.5f, 0.18f, 0.35f),
                        ProceduralArt.Cyan
                    );
                }
            }

            return root;
        }

        public void ResetWorld()
        {
            foreach (var pool in pools)
                pool.ReturnAll();

            nextZ = 160;
        }

        void Spawn(int lane, float z, int type)
        {
            if (Physics.CheckBox(
                new Vector3(GameConfig.LaneX(lane), 1, z),
                new Vector3(1.3f, 1, 14),
                Quaternion.identity,
                GameLayers.Sensors,
                QueryTriggerInteraction.Ignore))
            {
                return;
            }

            var go = pools[type].Rent();

            if (go == null)
                return;

            go.transform.position = new Vector3(
                GameConfig.LaneX(lane),
                0,
                z
            );

            go.SetActive(true);

            Physics.SyncTransforms();
        }

        void Update()
        {
            if (!game.IsPlaying)
                return;

            float playerZ = game.Player.transform.position.z;

            if (nextZ > playerZ + 210)
                return;

            int lane = Random.Range(0, 4);

            // random 3 loại:
            // 0 = Construction barrier
            // 1 = Pothole
            // 2 = Police roadblock
            int type = Random.Range(0, 3);

            Spawn(lane, nextZ, type);

            // Police roadblock có thể xuất hiện 2 cái
            if (type == 2 && game.Elapsed > 45)
            {
                Spawn((lane + 1) % 4, nextZ, type);
            }

            nextZ += Mathf.Lerp(82, 48, game.Difficulty);
        }

        public void Shift(float shift)
        {
            nextZ -= shift;
        }
    }
}