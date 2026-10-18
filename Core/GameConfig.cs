using UnityEngine;

namespace FinalFreedom
{
    // Toàn bộ thông số cân bằng nằm trong một asset, không cần sửa các controller.
    [CreateAssetMenu(menuName = "FinalFreedom/Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        [Header("Đường chạy / tốc độ (m/s)")]
        public float baseSpeed = 26f;
        public float maxSpeed = 42f;
        public float speedGainPerSecond = 0.055f;
        public float laneResponse = 7f;
        public float trafficInterval = 1.65f;
        public float minimumTrafficInterval = 0.65f;
        public int maxTraffic = 30;
        [Header("Traffic cone impact")]
        [Range(0.1f, 1f)] public float coneSpeedMultiplier = 0.6f;
        [Min(0)] public float coneSlowSeconds = 1.5f;
        [Header("Nitro / vật phẩm")]
        public float nitroMultiplier = 1.5f;
        public float nitroDrain = 12f;
        public float nitroRefill = 6f;
        public float magnetSeconds = 10f;
        public float boostSeconds = 8f;
        public float doubleScoreSeconds = 12f;
        [Header("Sát thương: k × khối lượng đối phương × vận tốc tương đối²")]
        public float damageK = 0.02f;
        [Tooltip("0.001: kg → tấn để cân bằng HP. Đặt 1 để dùng nguyên công thức trong đặc tả.")]
        public float damageMassScale = 0.001f;
        public float minDamage = 1f;
        public float maxDamage = 180f;
        public float minimumImpactSpeed = 2.5f;
        [Header("Cảnh sát")]
        [Range(0, 1)] public float policeFlawRate = 0.12f;
        public float policeSensor = 18f;
        public float policeRespawnMin = 10f;
        public float policeRespawnMax = 15f;
        [Header("Sương che rìa thành phố")]
        public Color fogColor = new Color(0.29f, 0.48f, 0.54f, 1f);
        [Min(0)] public float sideFogStart = 26f;
        [Min(1)] public float sideFogEnd = 31f;
        [Min(0)] public float distanceFogStart = 130f;
        [Min(1)] public float distanceFogEnd = 275f;
        [Header("Thiết bị")]
        public int targetFrameRate = 60;
        public int randomSeed = 12;

        public static float LaneX(int lane) => -4.5f + 3f * Mathf.Clamp(lane, 0, 3);
        public static int NearestLane(float x) => Mathf.Clamp(Mathf.RoundToInt((x + 4.5f) / 3f), 0, 3);
        public float Damage(float otherMass, float relativeSpeed)
        {
            if (relativeSpeed < minimumImpactSpeed) return 0;
            return Mathf.Clamp(damageK * otherMass * damageMassScale * relativeSpeed * relativeSpeed, minDamage, maxDamage);
        }
    }

    public enum VehicleKind { Motorcycle, Sedan, Truck, Bus, Container, Player, Police }
    public enum PickupKind { Coin, Magnet, Boost, Shield, Repair, DoubleScore }
    public enum RunState { Menu, Playing, Paused, GameOver }

    public static class GameLayers
    {
        public const int Player = 8, Police = 9, Traffic = 10, Obstacle = 11, Road = 12, Pickup = 13, Scenery = 14;
        public const int Sensors = (1 << Player) | (1 << Police) | (1 << Traffic) | (1 << Obstacle);
    }
}
