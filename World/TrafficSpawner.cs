using UnityEngine;

namespace FinalFreedom
{
    public sealed class TrafficSpawner : MonoBehaviour
    {
        ObjectPooler[] pools;
        GameManager game;
        float timer;
        public void Initialize(GameManager manager)
        {
            game = manager; pools = new ObjectPooler[5];
            for (int i = 0; i < pools.Length; i++)
            {
                var kind = (VehicleKind)i;
                pools[i] = new ObjectPooler(game.World, () =>
                {
                    var go = VehicleFactory.Create(kind);
                    go.AddComponent<TrafficVehicle>();
                    return go;
                }, 3, 8);
            }
        }
        public void ResetWorld() { foreach (var pool in pools) pool.ReturnAll(); timer = 0.8f; }
        public void SeedOpening()
        {
            Spawn(0, 74, 1);
            Spawn(3, 92, 2);
            Spawn(1, 145, 0);
            Spawn(2, 162, 1);
        }
        bool Spawn(int lane, float z, int kind)
        {
            // Kiểm tra cả xe đang active lẫn chốt chặn trước khi thuê object.
            if (Physics.CheckBox(new Vector3(GameConfig.LaneX(lane), 1, z), new Vector3(1.3f, 1, 14), Quaternion.identity, GameLayers.Sensors, QueryTriggerInteraction.Ignore)) return false;
            var go = pools[kind].Rent(); 
            if (go == null) return false;
            float speed = lane < 2 ? Random.Range(17f, 24f) : Random.Range(13f, 19f);
            if (kind == (int)VehicleKind.Container || kind == (int)VehicleKind.Truck) speed += 3;
            go.GetComponent<TrafficVehicle>().Spawn(lane, z, speed);
            Physics.SyncTransforms(); return true;
        }
        void Update()
        {
            if (!game.IsPlaying) return;
            timer -= Time.deltaTime; if (timer > 0) return;
            timer = Mathf.Lerp(game.Config.trafficInterval, game.Config.minimumTrafficInterval, game.Difficulty);
            int active = 0;
            foreach (var pool in pools) foreach (var go in pool.Items) if (go.activeSelf) active++;
            if (active >= game.Config.maxTraffic) return;
            int lane = Random.Range(0, 4);
            float z = game.Player.transform.position.z + Random.Range(155f, 220f);
            // Mỗi đợt chỉ thêm một xe, tránh hàng bốn xe bít toàn bộ đường.
            Spawn(lane, z, Random.Range(0, 5));
        }
        public void Shift(float shift) { }
    }
}
