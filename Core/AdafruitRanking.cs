using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace FinalFreedom
{
    [Serializable] public sealed class AdafruitScore { public int v; public string id, name; public int score; }
    [Serializable] public sealed class AdafruitDataPoint { public string id, value, created_at; }

    public static class AdafruitRanking
    {
        [Serializable] sealed class Page { public AdafruitDataPoint[] items; }
        public static bool Valid(AdafruitScore score) => score != null && score.v == 1 && score.score >= 0 &&
            Regex.IsMatch(score.id ?? "", @"\A[a-f0-9]{32}\z") && !string.IsNullOrWhiteSpace(score.name) &&
            score.name.Length <= 24 && Regex.IsMatch(score.name, @"\A[\p{L}\p{M}\p{N} _.'-]+\z");

        public static bool TryPoint(string json, out AdafruitScore score)
        {
            score = null;
            try
            {
                var point = JsonUtility.FromJson<AdafruitDataPoint>(json);
                if (string.IsNullOrEmpty(point?.value)) return false;
                score = JsonUtility.FromJson<AdafruitScore>(point.value);
                if (Valid(score)) return true;
            }
            catch (ArgumentException) { }
            score = null; return false;
        }

        public static bool TryPage(string json, out AdafruitScore[] scores, out AdafruitDataPoint[] points)
        {
            scores = null; points = null;
            try
            {
                if (string.IsNullOrWhiteSpace(json) || !json.TrimStart().StartsWith("[")) return false;
                var page = JsonUtility.FromJson<Page>("{\"items\":" + json + "}");
                if (page?.items == null) return false;
                var result = new List<AdafruitScore>();
                foreach (var point in page.items)
                {
                    if (string.IsNullOrEmpty(point?.value)) return false;
                    var score = JsonUtility.FromJson<AdafruitScore>(point.value);
                    if (!Valid(score)) return false;
                    result.Add(score);
                }
                scores = result.ToArray(); points = page.items; return true;
            }
            catch (ArgumentException) { return false; }
        }

        public static RankingResponse Rank(IEnumerable<AdafruitScore> scores)
        {
            // Append-only events make concurrent writers and delayed retries safe.
            var best = new Dictionary<string, AdafruitScore>();
            foreach (var score in scores)
            {
                if (!Valid(score)) continue;
                if (!best.TryGetValue(score.id, out var old) || score.score > old.score ||
                    (score.score == old.score && string.CompareOrdinal(score.name, old.name) < 0)) best[score.id] = score;
            }
            return new RankingResponse { cloudSynced = true, entries = best.Values.OrderByDescending(s => s.score)
                .ThenBy(s => s.name, StringComparer.OrdinalIgnoreCase).ThenBy(s => s.id, StringComparer.Ordinal)
                .Take(100).Select((s, i) => new RankingEntry { top = i + 1, id = s.id, name = s.name, score = s.score }).ToArray() };
        }

        public static string OlderPage(string endpoint, AdafruitDataPoint[] points, ref DateTimeOffset? previousEnd)
        {
            if (points.Length < 1000) return null;
            // Adafruit does not expose Link through CORS. Overlapping time windows also work in WebGL.
            // Overlap one second so equal timestamps are never omitted; Rank removes duplicate events.
            DateTimeOffset oldest = DateTimeOffset.MaxValue;
            foreach (var point in points)
            {
                if (!DateTimeOffset.TryParse(point.created_at, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var time))
                    throw new FormatException("Missing event timestamp.");
                if (time < oldest) oldest = time;
            }
            var end = oldest.AddSeconds(1);
            if (previousEnd.HasValue && end >= previousEnd.Value)
                throw new FormatException("Too many events in one time window; keep cached ranking.");
            previousEnd = end;
            return endpoint + "?limit=1000&end_time=" + Uri.EscapeDataString(end.UtcDateTime.ToString("o", CultureInfo.InvariantCulture));
        }

        public static string Table(RankingEntry[] entries)
        {
            var text = new StringBuilder("Top   | Name                     |      Score\n-----------------------------------------------");
            foreach (var row in entries) text.AppendFormat(CultureInfo.InvariantCulture, "\n{0,-5} | {1,-24} | {2,10}", row.top, row.name, row.score);
            if (entries.Length == 0) text.Append("\nNo scores yet. Play your first run!");
            return text.ToString();
        }
    }
}
