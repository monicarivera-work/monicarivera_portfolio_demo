using Azure;
using Azure.Data.Tables;
using PortfolioDemo.Models;
using System.Web;

namespace PortfolioDemo.Services
{
    public interface ISessionService
    {
        Task<List<CodeSessionDto>> GetSessionsAsync(string userOid);
        Task<CodeSessionDto?> GetSessionAsync(string userOid, string sessionName);
        Task SaveSessionAsync(string userOid, SaveSessionRequest request);
        Task DeleteSessionAsync(string userOid, string sessionName);
    }

    public class SessionService : ISessionService
    {
        private const string TableName = "CodeLabSessions";
        private readonly TableClient? _tableClient;
        private readonly ILogger<SessionService> _logger;
        private bool _isConfigured => _tableClient != null;

        public SessionService(IConfiguration configuration, ILogger<SessionService> logger)
        {
            _logger = logger;
            var connectionString = configuration[Constants.AzureStorageConnectionStringKey];
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                _logger.LogWarning("Azure Storage connection string not configured; session persistence is disabled.");
                _tableClient = null;
                return;
            }

            _tableClient = new TableClient(connectionString, TableName);
            _tableClient.CreateIfNotExists();
        }

        public async Task<List<CodeSessionDto>> GetSessionsAsync(string userOid)
        {
            if (!_isConfigured) return new List<CodeSessionDto>();
            try
            {
                var sessions = new List<CodeSessionDto>();
                await foreach (var entity in _tableClient!.QueryAsync<CodeSessionEntity>(
                    e => e.PartitionKey == userOid))
                {
                    sessions.Add(MapToDto(entity));
                }
                return sessions.OrderByDescending(s => s.UpdatedAt).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve sessions for user {OID}", userOid);
                return new List<CodeSessionDto>();
            }
        }

        public async Task<CodeSessionDto?> GetSessionAsync(string userOid, string sessionName)
        {
            if (!_isConfigured) return null;
            try
            {
                var rowKey = EncodeKey(sessionName);
                var response = await _tableClient!.GetEntityAsync<CodeSessionEntity>(userOid, rowKey);
                return MapToDto(response.Value);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve session for user {OID} (name length: {NameLen})",
                    userOid, sessionName?.Length ?? 0);
                return null;
            }
        }

        public async Task SaveSessionAsync(string userOid, SaveSessionRequest request)
        {
            if (!_isConfigured) return;
            var rowKey = EncodeKey(request.SessionName);
            var now = DateTimeOffset.UtcNow;

            CodeSessionEntity entity;
            try
            {
                var existing = await _tableClient!.GetEntityAsync<CodeSessionEntity>(userOid, rowKey);
                entity = existing.Value;
                entity.Code = request.Code;
                entity.Language = request.Language;
                entity.Description = request.Description;
                entity.UpdatedAt = now;
                await _tableClient.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                entity = new CodeSessionEntity
                {
                    PartitionKey = userOid,
                    RowKey = rowKey,
                    SessionName = request.SessionName,
                    Code = request.Code,
                    Language = request.Language,
                    Description = request.Description,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                await _tableClient!.AddEntityAsync(entity);
            }
        }

        public async Task DeleteSessionAsync(string userOid, string sessionName)
        {
            if (!_isConfigured) return;
            try
            {
                var rowKey = EncodeKey(sessionName);
                await _tableClient!.DeleteEntityAsync(userOid, rowKey);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                // Already gone, that's fine
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete session for user {OID} (name length: {NameLen})",
                    userOid, sessionName?.Length ?? 0);
            }
        }

        private static string EncodeKey(string name) =>
            HttpUtility.UrlEncode(name.Trim()).Replace("+", "%20");

        private static CodeSessionDto MapToDto(CodeSessionEntity e) => new()
        {
            SessionName = e.SessionName,
            Code = e.Code,
            Language = e.Language,
            Description = e.Description,
            UpdatedAt = e.UpdatedAt
        };
    }
}
