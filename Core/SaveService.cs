using UnityEngine;

namespace FinalFreedom
{
    // Lưu cục bộ, không đăng nhập và không có mua hàng bằng tiền thật.
    public static class SaveService
    {
        const string Prefix = "FinalFreedom.v1.";
        public static int Wallet => PlayerPrefs.GetInt(Prefix + "wallet", 0);
        public static int Best => PlayerPrefs.GetInt(Prefix + "best", 0);
        public const string NameKey = "FinalFreedom.v3.name";
        public const string PlayerIdKey = "FinalFreedom.v3.playerId";
        public const string PlayerTokenKey = "FinalFreedom.v3.playerToken";
        public const string UploadedScoreKey = "FinalFreedom.v3.uploadedScore";
        public const string RankingCacheKey = "FinalFreedom.v3.rankingCache";
        public static string PlayerName => PlayerPrefs.GetString(NameKey, "");
        public static bool HasPlayerName => !string.IsNullOrEmpty(PlayerName);
        public static string PlayerId => PlayerPrefs.GetString(PlayerIdKey, "");
        public static string PlayerToken => PlayerPrefs.GetString(PlayerTokenKey, "");
        public static int UploadedScore => PlayerPrefs.GetInt(UploadedScoreKey, -1);

        public static bool TrySetPlayerName(string input)
        {
            if (HasPlayerName) return false;
            string name = System.Text.RegularExpressions.Regex.Replace((input ?? "").Normalize(), @"\s+", " ").Trim();
            if (name.Length < 1 || name.Length > 24 || !System.Text.RegularExpressions.Regex.IsMatch(name, @"\A[\p{L}\p{M}\p{N} _.'-]+\z")) return false;
            PlayerPrefs.SetString(NameKey, name);
            PlayerPrefs.SetString(PlayerIdKey, System.Guid.NewGuid().ToString("N"));
            PlayerPrefs.SetString(PlayerTokenKey, System.Guid.NewGuid().ToString("N") + System.Guid.NewGuid().ToString("N"));
            PlayerPrefs.SetInt(UploadedScoreKey, -1);
            PlayerPrefs.Save();
            return true;
        }

        public static void AcknowledgeScore(int score)
        {
            PlayerPrefs.SetInt(UploadedScoreKey, Mathf.Max(UploadedScore, score));
            PlayerPrefs.Save();
        }
        public const string VehicleKey = "FinalFreedom.v2.vehicle";
        public static int Selected
        {
            get
            {
                var catalog = VehicleCatalog.Instance;
                int index = catalog.IndexOf(PlayerPrefs.GetString(VehicleKey, ""));
                if (index >= 0) return index;
                // Migrate the original choices by prefab, independently of the catalog order.
                if (!PlayerPrefs.HasKey(VehicleKey))
                {
                    var legacy = CityPrefabLibrary.Instance.playerCars;
                    if (legacy.Length > 0)
                    {
                        int old = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "selected", 0), 0, legacy.Length - 1);
                        index = System.Array.FindIndex(catalog.vehicles, vehicle => vehicle.prefab == legacy[old]);
                        if (index >= 0) return index;
                    }
                }
                return 0;
            }
        }
        public static PlayerVehicle CurrentVehicle => VehicleCatalog.Instance.vehicles[Selected];
        public static bool SelectVehicle(int index)
        {
            var vehicles = VehicleCatalog.Instance.vehicles;
            if (index < 0 || index >= vehicles.Length) return false;
            // All vehicles are available; wallet and records are preserved.
            PlayerPrefs.SetString(VehicleKey, vehicles[index].id);
            PlayerPrefs.Save();
            return true;
        }
        public static void BankRun(int coins, int score)
        {
            PlayerPrefs.SetInt(Prefix + "wallet", Wallet + Mathf.Max(0, coins));
            PlayerPrefs.SetInt(Prefix + "best", Mathf.Max(Best, score));
            PlayerPrefs.Save();
        }
    }
}
