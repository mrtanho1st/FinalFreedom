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
            // 1 = Police roadblock
            pools = new ObjectPooler[2];

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
            var root = new GameObject(type == 0 ? "Construction barrier" : "Police roadblock");
            root.layer = GameLayers.Obstacle;
            root.AddComponent<RoadObstacle>();

            // Keep the original solid collider and layer so damage, sensors and boost behave as before.
            var box = root.AddComponent<BoxCollider>();
            box.sharedMaterial = ProceduralArt.VehiclePhysics;
            box.size = new Vector3(2.55f, 1.1f, 0.65f);
            box.center = new Vector3(0, 0.55f, 0);

            var library = CityPrefabLibrary.Instance;
            PrefabArt.Module(type == 0 ? library.constructionBarrier : library.policeRoadblock,
                root.transform, "Barrier model", box.size, new Vector3(0, box.size.y, 0));
            if (type == 1)
            {
                // Visual markers only; keep them inside the existing collision footprint.
                for (int side = -1; side <= 1; side += 2)
                {
                    var cone = PrefabArt.Instantiate(library.roadblockCone, root.transform);
                    PrefabArt.Fit(cone, new Vector3(0.3f, 0.55f, 0.3f), new Vector3(side * 1.05f, 0, -0.15f));
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

            // Choose from the two remaining solid obstacle types.
            int type = Random.Range(0, pools.Length);

            Spawn(lane, nextZ, type);

            // Police roadblock có thể xuất hiện 2 cái
            if (type == 1 && game.Elapsed > 45)
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
