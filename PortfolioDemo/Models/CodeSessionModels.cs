using Azure;
using Azure.Data.Tables;

namespace PortfolioDemo.Models
{
    public class CodeSessionEntity : ITableEntity
    {
        // PartitionKey = user OID, RowKey = session name (url-encoded)
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string SessionName { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Language { get; set; } = "javascript";
        public string? Description { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }

    public class CodeSessionDto
    {
        public string SessionName { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Language { get; set; } = "javascript";
        public string? Description { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }

    public class SaveSessionRequest
    {
        public string SessionName { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Language { get; set; } = "javascript";
        public string? Description { get; set; }
    }
}
