using UnityEngine;

namespace FinalFreedom
{
    public sealed class LeaderboardConfig : ScriptableObject
    {
        [Tooltip("Public HTTPS relay URL. Never put an Adafruit key in this asset.")]
        public string serverUrl = "";
        public string editorServerUrl = "http://127.0.0.1:8787";
        public bool networkEnabled = true;
        public string dashboardUrl = "https://io.adafruit.com/LeMinhTan/dashboards/final-freedom-leaderboard";
        public string Url => (Application.isEditor && string.IsNullOrWhiteSpace(serverUrl) ? editorServerUrl : serverUrl).TrimEnd('/');
    }
}
