using UnityEngine;

namespace FinalFreedom
{
    public sealed class LeaderboardConfig : ScriptableObject
    {
        [Tooltip("Public HTTPS relay URL. Never put an Adafruit key in this asset.")]
        public string serverUrl = "";
        public string editorServerUrl = "http://127.0.0.1:8787";
        public bool networkEnabled = true;
        [Tooltip("Read and write Adafruit directly. The write key is entered at runtime, never bundled.")]
        public bool useDirectAdafruit = true;
        public string adafruitUsername = "LeMinhTan";
        public string adafruitScoresFeed = "final-freedom-direct-scores";
        public string adafruitRankingFeed = "final-freedom-direct-ranking";
        public string dashboardUrl = "https://io.adafruit.com/LeMinhTan/dashboards/final-freedom-direct-leaderboard";
        string FeedUrl(string feed) => "https://io.adafruit.com/api/v2/" + System.Uri.EscapeDataString(adafruitUsername ?? "") +
            "/feeds/" + System.Uri.EscapeDataString(feed ?? "") + "/data";
        public string ScoresUrl => FeedUrl(adafruitScoresFeed);
        public string TableUrl => FeedUrl(adafruitRankingFeed);
        public string RankingUrl => useDirectAdafruit ? ScoresUrl + "?limit=1000" : Url + "/leaderboard";
        public string Url
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                // Docker forwards same-origin requests to the existing HTTPS relay; no browser CORS dependency.
                return new System.Uri(new System.Uri(Application.absoluteURL), "api/leaderboard").AbsoluteUri.TrimEnd('/');
#else
                return (Application.isEditor && string.IsNullOrWhiteSpace(serverUrl) ? editorServerUrl : serverUrl).TrimEnd('/');
#endif
            }
        }
    }
}
