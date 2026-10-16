using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace FinalFreedom
{
    [Serializable] public sealed class RankingEntry { public int top; public string id; public string name; public int score; }
    [Serializable] public sealed class RankingResponse { public RankingEntry[] entries = Array.Empty<RankingEntry>(); public bool cloudSynced; }
    [Serializable] sealed class ScoreRequest { public string id; public string token; public string name; public int score; }
    [Serializable] sealed class ScoreResponse { public bool accepted; public int best; public bool cloudSynced; }

    public sealed class LeaderboardClient : MonoBehaviour
    {
        public LeaderboardConfig Config { get; private set; }
        public RankingEntry[] Entries { get; private set; } = Array.Empty<RankingEntry>();
        public string Status { get; private set; } = "";
        public event Action Changed;
        bool busy;
        bool submittedThisSession;
        float retryAt, refreshAt;
        UnityWebRequest active;

        public void Initialize()
        {
            Config = Resources.Load<LeaderboardConfig>("LeaderboardConfig");
            try
            {
                var cached = JsonUtility.FromJson<RankingResponse>(PlayerPrefs.GetString(SaveService.RankingCacheKey, "{}"));
                Entries = cached?.entries ?? Array.Empty<RankingEntry>();
            }
            catch (ArgumentException) { Entries = Array.Empty<RankingEntry>(); }
            retryAt = Time.unscaledTime + 2;
        }
        bool Available => Config != null && Config.networkEnabled && Uri.TryCreate(Config.Url, UriKind.Absolute, out var uri)
            && (uri.Scheme == "https" || (uri.Scheme == "http" && uri.IsLoopback));
        void Notify(string status) { Status = status; Changed?.Invoke(); }
        public void QueueBest() { retryAt = 0; }
        void Update()
        {
            if (busy || !Available || !SaveService.HasPlayerName || Time.unscaledTime < retryAt) return;
            if (!submittedThisSession || SaveService.UploadedScore < SaveService.Best) StartCoroutine(SendBest());
        }
        public void Refresh()
        {
            if (!Available) { Notify("ONLINE LEADERBOARD IS NOT CONFIGURED. YOUR BEST IS SAVED ON THIS DEVICE."); return; }
            if (busy || Time.unscaledTime < refreshAt) return;
            StartCoroutine(Fetch());
        }
        IEnumerator SendBest()
        {
            busy = true;
            int score = SaveService.Best;
            var payload = new ScoreRequest { id = SaveService.PlayerId, token = SaveService.PlayerToken, name = SaveService.PlayerName, score = score };
            using (var request = new UnityWebRequest(Config.Url + "/scores", "POST"))
            {
                active = request;
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload)));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 12;
                Notify("SAVING PERSONAL BEST...");
                yield return request.SendWebRequest();
                active = null;
                bool accepted = false;
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var result = JsonUtility.FromJson<ScoreResponse>(request.downloadHandler.text);
                        accepted = result != null && result.accepted && result.best >= score;
                    }
                    catch (ArgumentException) { }
                }
                if (accepted) { SaveService.AcknowledgeScore(score); submittedThisSession = true; }
                else Notify("OFFLINE / SCORE SAVED ON DEVICE. WILL RETRY AUTOMATICALLY.");
                retryAt = Time.unscaledTime + (accepted ? 2 : 30);
                busy = false;
                if (accepted) yield return Fetch();
            }
        }
        IEnumerator Fetch()
        {
            busy = true;
            refreshAt = Time.unscaledTime + 5;
            Notify("LOADING LEADERBOARD...");
            using (var request = UnityWebRequest.Get(Config.Url + "/leaderboard"))
            {
                active = request; request.timeout = 12;
                yield return request.SendWebRequest();
                active = null;
                RankingResponse result = null;
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try { result = JsonUtility.FromJson<RankingResponse>(request.downloadHandler.text); }
                    catch (ArgumentException) { }
                }
                if (result?.entries != null)
                {
                    Entries = result.entries;
                    PlayerPrefs.SetString(SaveService.RankingCacheKey, JsonUtility.ToJson(result)); PlayerPrefs.Save();
                    Notify(result.cloudSynced ? "UPDATED / ADAFRUIT IO SYNCED" : "UPDATED / WAITING FOR ADAFRUIT IO SYNC");
                }
                else Notify("OFFLINE / SHOWING LAST SAVED LEADERBOARD");
            }
            busy = false;
        }
        void OnDestroy()
        {
            if (active != null) active.Abort();
            StopAllCoroutines();
        }
    }
}
