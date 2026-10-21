using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

namespace FinalFreedom
{
    [Serializable] public sealed class RankingEntry { public int top; public string id; public string name; public int score; }
    [Serializable] public sealed class RankingResponse { public RankingEntry[] entries = Array.Empty<RankingEntry>(); public bool cloudSynced; }
    [Serializable] sealed class ScoreRequest { public string id, token, name; public int score; }
    [Serializable] sealed class ScoreResponse { public bool accepted; public int best; public bool cloudSynced; }

    public sealed class LeaderboardClient : MonoBehaviour
    {
        public LeaderboardConfig Config { get; private set; }
        public RankingEntry[] Entries { get; private set; } = Array.Empty<RankingEntry>();
        public string Status { get; private set; } = "";
        public string SubmissionStatus { get; private set; } = "";
        public bool HasWriteKey => !string.IsNullOrEmpty(writeKey);
        public event Action Changed;
        // Session memory only. Never serialize the account key or put it in PlayerPrefs/URLs/logs.
        string writeKey;
        bool sending, fetching, submittedThisSession, tablePending, tableReady;
        int writeGeneration, acceptedLocalScore;
        float retryAt, refreshAt, tableRetryAt;
        float refreshAfterSubmission = float.PositiveInfinity;
        UnityWebRequest activeRead, activeWrite;

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
            if (Config != null && Config.useDirectAdafruit) SubmissionStatus = "CONNECT ADAFRUIT TO UPLOAD YOUR BEST. VIEWING IS PUBLIC.";
        }
        static bool AllowedUrl(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == "https" || (uri.Scheme == "http" && uri.IsLoopback));
        bool CanSubmit => Config != null && Config.networkEnabled && (Config.useDirectAdafruit ? HasWriteKey && CanRead : AllowedUrl(Config.Url));
        bool CanRead => Config != null && Config.networkEnabled && AllowedUrl(Config.RankingUrl) &&
            (!Config.useDirectAdafruit || (!string.IsNullOrWhiteSpace(Config.adafruitUsername) && !string.IsNullOrWhiteSpace(Config.adafruitScoresFeed)));
        void Notify(string status) { Status = status; Changed?.Invoke(); }
        void NotifySubmission(string status) { SubmissionStatus = status; Changed?.Invoke(); }

        public bool ConnectAdafruit(string key)
        {
            if (sending || !Regex.IsMatch((key ?? "").Trim(), @"\Aaio_[A-Za-z0-9]{20,128}\z")) return false;
            writeKey = key.Trim(); submittedThisSession = false; retryAt = Time.unscaledTime;
            NotifySubmission("CONNECTING / SAVING YOUR PERSONAL BEST...");
            return true;
        }
        public void DisconnectAdafruit()
        {
            writeKey = null;
            activeWrite?.Abort();
            tablePending = tableReady = false;
            NotifySubmission("KEY REMOVED / YOUR BEST IS SAVED ON THIS DEVICE.");
        }
        // Update detects changed best scores. Do not shorten a rate-limit/offline backoff.
        public void QueueBest() { retryAt = Mathf.Max(retryAt, Time.unscaledTime + 1); }
        void Update()
        {
            if (!fetching && CanRead && Time.unscaledTime >= refreshAfterSubmission)
            {
                refreshAfterSubmission = float.PositiveInfinity;
                Refresh();
            }
            if (sending || !CanSubmit || !SaveService.HasPlayerName) return;
            if (Time.unscaledTime >= retryAt && (!submittedThisSession || SaveService.UploadedScore < SaveService.Best))
                StartCoroutine(SendBest());
            else if (tablePending && tableReady && !fetching && Time.unscaledTime >= tableRetryAt)
                StartCoroutine(PublishTable());
        }
        public void Refresh()
        {
            if (!CanRead) { Notify("ONLINE LEADERBOARD IS NOT CONFIGURED. YOUR BEST IS SAVED ON THIS DEVICE."); return; }
            if (fetching) return;
            if (Time.unscaledTime < refreshAt) { refreshAfterSubmission = refreshAt; return; }
            StartCoroutine(Fetch());
        }
        UnityWebRequest Post(string url, string json, bool direct)
        {
            var request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            if (direct) request.SetRequestHeader("X-AIO-Key", writeKey);
            request.redirectLimit = 0; request.timeout = 15;
            return request;
        }
        static string Value(string text) => JsonUtility.ToJson(new AdafruitDataPoint { value = text });
        void WriteFailure(long code)
        {
            if (code == 401 || code == 403)
            {
                writeKey = null;
                NotifySubmission("KEY REJECTED / OPEN CONNECT ADAFRUIT AND ENTER A VALID AIO KEY.");
            }
            else NotifySubmission(code == 429 ? "ADAFRUIT RATE LIMIT / RETRYING IN 60 SECONDS." :
                "UPLOAD FAILED / BEST SAVED ON DEVICE. RETRYING AUTOMATICALLY.");
        }
        IEnumerator SendBest()
        {
            sending = true;
            bool direct = Config.useDirectAdafruit;
            int score = SaveService.Best;
            var point = new AdafruitScore { v = 1, id = SaveService.PlayerId, name = SaveService.PlayerName, score = score };
            string json = direct ? Value(JsonUtility.ToJson(point)) : JsonUtility.ToJson(new ScoreRequest {
                id = SaveService.PlayerId, token = SaveService.PlayerToken, name = SaveService.PlayerName, score = score });
            try
            {
                using (var request = Post(direct ? Config.ScoresUrl : Config.Url + "/scores", json, direct))
                {
                    activeWrite = request; NotifySubmission("SAVING PERSONAL BEST...");
                    yield return request.SendWebRequest();
                    bool accepted = false;
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        if (direct) accepted = AdafruitRanking.TryPoint(request.downloadHandler.text, out var saved) &&
                            saved.id == point.id && saved.name == point.name && saved.score == score;
                        else
                        {
                            try { var result = JsonUtility.FromJson<ScoreResponse>(request.downloadHandler.text); accepted = result != null && result.accepted && result.best >= score; }
                            catch (ArgumentException) { }
                        }
                    }
                    if (accepted)
                    {
                        SaveService.AcknowledgeScore(score); submittedThisSession = true;
                        NotifySubmission(direct ? "BEST SAVED DIRECTLY TO ADAFRUIT." : "PERSONAL BEST SAVED.");
                        if (direct) { acceptedLocalScore = score; tablePending = true; tableReady = false; writeGeneration++; tableRetryAt = Time.unscaledTime + 3; }
                        refreshAfterSubmission = Time.unscaledTime + 1;
                    }
                    else WriteFailure(request.responseCode);
                    retryAt = Time.unscaledTime + (accepted ? 6 : request.responseCode == 429 ? 60 : 30);
                }
            }
            finally { activeWrite = null; sending = false; }
        }
        IEnumerator PublishTable()
        {
            sending = true;
            try
            {
                using (var request = Post(Config.TableUrl, Value(AdafruitRanking.Table(Entries)), true))
                {
                    activeWrite = request; yield return request.SendWebRequest();
                    if (request.result == UnityWebRequest.Result.Success)
                    { tablePending = false; NotifySubmission("BEST SAVED / ADAFRUIT DASHBOARD UPDATED."); }
                    else
                    {
                        if (request.responseCode == 401 || request.responseCode == 403) WriteFailure(request.responseCode);
                        else NotifySubmission("BEST SAVED / DASHBOARD UPDATE WILL RETRY.");
                        // Refetch before retrying the presentation cache. Never replay an old table.
                        tableReady = false;
                        tableRetryAt = Time.unscaledTime + 60;
                        refreshAfterSubmission = tableRetryAt;
                    }
                }
            }
            finally { activeWrite = null; sending = false; }
        }
        IEnumerator Fetch()
        {
            fetching = true;
            bool direct = Config.useDirectAdafruit;
            int generation = writeGeneration;
            refreshAt = Time.unscaledTime + 5;
            Notify("LOADING LEADERBOARD...");
            string url = Config.RankingUrl, failure = "OFFLINE OR INVALID DATA / SHOWING SAVED LEADERBOARD";
            var scores = new List<AdafruitScore>();
            RankingResponse result = null;
            DateTimeOffset? previousEnd = null;
            try
            {
                for (int page = 0; url != null && page < 100; page++)
                {
                    using (var request = UnityWebRequest.Get(url))
                    {
                        activeRead = request; request.timeout = 15;
                        yield return request.SendWebRequest();
                        if (request.result != UnityWebRequest.Result.Success)
                        {
                            if (request.responseCode == 429) { failure = "ADAFRUIT RATE LIMIT / SHOWING SAVED LEADERBOARD"; refreshAt = Time.unscaledTime + 60; }
                            else if (request.responseCode == 404 || request.responseCode == 401 || request.responseCode == 403)
                                failure = "ADAFRUIT FEED UNAVAILABLE OR PRIVATE / SHOWING SAVED LEADERBOARD";
                            break;
                        }
                        try
                        {
                            if (!direct) { result = JsonUtility.FromJson<RankingResponse>(request.downloadHandler.text); url = null; }
                            else if (AdafruitRanking.TryPage(request.downloadHandler.text, out var batch, out var points))
                            {
                                scores.AddRange(batch);
                                url = AdafruitRanking.OlderPage(Config.ScoresUrl, points, ref previousEnd);
                                if (url == null) result = AdafruitRanking.Rank(scores);
                            }
                            else break;
                        }
                        catch (Exception error) when (error is ArgumentException || error is FormatException)
                        { break; }
                    }
                    activeRead = null;
                    if (url != null) yield return new WaitForSecondsRealtime(0.25f);
                }
                if (result?.entries != null)
                {
                    Entries = result.entries;
                    PlayerPrefs.SetString(SaveService.RankingCacheKey, JsonUtility.ToJson(result)); PlayerPrefs.Save();
                    if (direct && generation == writeGeneration)
                    {
                        tableReady = !tablePending || scores.Exists(s => s.id == SaveService.PlayerId && s.score >= acceptedLocalScore);
                        if (tablePending && !tableReady) refreshAfterSubmission = Time.unscaledTime + 6;
                    }
                    Notify(direct ? "UPDATED / DIRECT FROM ADAFRUIT IO" : result.cloudSynced ? "UPDATED / ADAFRUIT IO SYNCED" : "UPDATED / WAITING FOR ADAFRUIT IO SYNC");
                }
                else
                {
                    Notify(failure);
                    if (tablePending) refreshAfterSubmission = Mathf.Max(refreshAt, Time.unscaledTime + 30);
                }
            }
            finally { activeRead = null; fetching = false; }
        }
        void OnDestroy()
        {
            writeKey = null;
            activeRead?.Abort(); activeWrite?.Abort(); StopAllCoroutines();
        }
    }
}
