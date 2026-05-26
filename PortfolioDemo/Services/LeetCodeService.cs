using PortfolioDemo.Models;
using System.Text;
using System.Text.Json;

namespace PortfolioDemo.Services
{
    public interface ILeetCodeService
    {
        Task<LeetCodeStats?> GetUserStatsAsync(string username);
    }

    public class LeetCodeService : ILeetCodeService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<LeetCodeService> _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public LeetCodeService(IHttpClientFactory httpClientFactory, ILogger<LeetCodeService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<LeetCodeStats?> GetUserStatsAsync(string username)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("LeetCode");

                var query = new
                {
                    query = @"
                        query getUserProfile($username: String!) {
                            matchedUser(username: $username) {
                                username
                                profile {
                                    ranking
                                    userAvatar
                                    realName
                                }
                                submitStats: submitStatsGlobal {
                                    acSubmissionNum {
                                        difficulty
                                        count
                                        submissions
                                    }
                                }
                            }
                            allQuestionsCount {
                                difficulty
                                count
                            }
                            recentAcSubmissionList(username: $username, limit: 10) {
                                id
                                title
                                titleSlug
                                timestamp
                                lang
                            }
                        }",
                    variables = new { username }
                };

                var json = JsonSerializer.Serialize(query);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("https://leetcode.com/graphql", content);
                response.EnsureSuccessStatusCode();

                var body = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(body);
                var data = doc.RootElement.GetProperty("data");

                var stats = new LeetCodeStats { Username = username };

                // Parse profile / ranking
                if (data.TryGetProperty("matchedUser", out var user) && user.ValueKind == JsonValueKind.Object)
                {
                    if (user.TryGetProperty("profile", out var profile))
                    {
                        if (profile.TryGetProperty("ranking", out var rank))
                            stats.Ranking = rank.GetInt32();
                        if (profile.TryGetProperty("userAvatar", out var avatar))
                            stats.UserAvatar = avatar.GetString();
                        if (profile.TryGetProperty("realName", out var name))
                            stats.RealName = name.GetString();
                    }

                    if (user.TryGetProperty("submitStats", out var submitStats) &&
                        submitStats.TryGetProperty("acSubmissionNum", out var acNums))
                    {
                        foreach (var item in acNums.EnumerateArray())
                        {
                            var difficulty = item.GetProperty("difficulty").GetString();
                            var count = item.GetProperty("count").GetInt32();
                            switch (difficulty)
                            {
                                case "All": stats.TotalSolved = count; break;
                                case "Easy": stats.EasySolved = count; break;
                                case "Medium": stats.MediumSolved = count; break;
                                case "Hard": stats.HardSolved = count; break;
                            }
                        }
                    }

                    // Parse recent submissions
                    if (data.TryGetProperty("recentAcSubmissionList", out var submissions))
                    {
                        foreach (var sub in submissions.EnumerateArray())
                        {
                            stats.RecentSubmissions.Add(new RecentSubmission
                            {
                                Id = sub.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "",
                                Title = sub.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
                                TitleSlug = sub.TryGetProperty("titleSlug", out var ts) ? ts.GetString() ?? "" : "",
                                Timestamp = sub.TryGetProperty("timestamp", out var ts2) ? long.Parse(ts2.GetString() ?? "0") : 0,
                                Language = sub.TryGetProperty("lang", out var lang) ? lang.GetString() ?? "" : ""
                            });
                        }
                    }
                }

                // Parse total question counts
                if (data.TryGetProperty("allQuestionsCount", out var allQ))
                {
                    foreach (var item in allQ.EnumerateArray())
                    {
                        var difficulty = item.GetProperty("difficulty").GetString();
                        var count = item.GetProperty("count").GetInt32();
                        switch (difficulty)
                        {
                            case "All": stats.TotalQuestions = count; break;
                            case "Easy": stats.TotalEasy = count; break;
                            case "Medium": stats.TotalMedium = count; break;
                            case "Hard": stats.TotalHard = count; break;
                        }
                    }
                }

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch LeetCode stats for {Username}", username);
                return null;
            }
        }
    }
}
