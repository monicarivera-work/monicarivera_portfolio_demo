namespace PortfolioDemo.Models
{
    public class LeetCodeStats
    {
        public string Username { get; set; } = string.Empty;
        public int Ranking { get; set; }
        public int TotalSolved { get; set; }
        public int EasySolved { get; set; }
        public int MediumSolved { get; set; }
        public int HardSolved { get; set; }
        public int TotalEasy { get; set; }
        public int TotalMedium { get; set; }
        public int TotalHard { get; set; }
        public int TotalQuestions { get; set; }
        public string? UserAvatar { get; set; }
        public string? RealName { get; set; }
        public List<RecentSubmission> RecentSubmissions { get; set; } = new();
    }

    public class RecentSubmission
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string TitleSlug { get; set; } = string.Empty;
        public long Timestamp { get; set; }
        public string Language { get; set; } = string.Empty;
    }
}
