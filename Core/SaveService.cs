using UnityEngine;

namespace FinalFreedom
{
    // Lưu cục bộ, không đăng nhập và không có mua hàng bằng tiền thật.
    public static class SaveService
    {
        const string Prefix = "FinalFreedom.v1.";
        public static int Wallet => PlayerPrefs.GetInt(Prefix + "wallet", 0);
        public static int Best => PlayerPrefs.GetInt(Prefix + "best", 0);
        public static int Selected => Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "selected", 0), 0, 2);
        public static readonly string[] Names = { "STREET RUNNER", "INTERCEPTOR", "ARMORED GT" };
        public static readonly int[] Prices = { 0, 150, 400 };
        public static readonly float[] Health = { 100f, 120f, 160f };
        public static readonly float[] SpeedBonus = { 0f, 2f, 0f };
        public static bool Owned(int index) => index == 0 || PlayerPrefs.GetInt(Prefix + "car" + index, 0) == 1;
        public static bool BuyOrSelect(int index)
        {
            if (index < 0 || index >= Prices.Length) return false;
            if (!Owned(index))
            {
                if (Wallet < Prices[index]) return false;
                PlayerPrefs.SetInt(Prefix + "wallet", Wallet - Prices[index]);
                PlayerPrefs.SetInt(Prefix + "car" + index, 1);
            }
            PlayerPrefs.SetInt(Prefix + "selected", index);
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
