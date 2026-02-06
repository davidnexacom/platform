using Dapper;
using FSH.ExternalGateway.Configuration;
using FSH.ExternalGateway.Messages;
using FSH.ExternalGateway.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rebus.Bus;
using Rebus.Handlers;
using System.Diagnostics;
using System.Text.Json;

namespace FSH.ExternalGateway.Handlers;

/// <summary>
/// Handler for dynamic SQL queries sent from the API.
/// Executes SQL with parameters and returns results as JSON.
/// </summary>
public sealed class DynamicSqlQueryHandler : IHandleMessages<DynamicSqlQueryRequest>
{
    private readonly GatewayOptions _options;
    private readonly IQueryRepository _queryRepository;
    private readonly IBus _bus;
    private readonly ILogger<DynamicSqlQueryHandler> _logger;

    public DynamicSqlQueryHandler(
        IOptions<GatewayOptions> options,
        IQueryRepository queryRepository,
        IBus bus,
        ILogger<DynamicSqlQueryHandler> logger)
    {
        _options = options.Value;
        _queryRepository = queryRepository;
        _bus = bus;
        _logger = logger;
    }

    public async Task Handle(DynamicSqlQueryRequest message)
    {
        _logger.LogInformation(
            "Processing DynamicSqlQuery for Client: {ClientId}, RegisteredQuery: {QueryName}, HasCustomSQL: {HasSql}",
            message.ClientId,
            message.RegisteredQueryName ?? "none",
            !string.IsNullOrEmpty(message.SqlQuery));

        var sw = Stopwatch.StartNew();

        try
        {
            // Get SQL query from registered repository or from message
            string sql;
            if (!string.IsNullOrEmpty(message.RegisteredQueryName))
            {
                sql = _queryRepository.GetQuery(message.RegisteredQueryName, message.SchemaVersion)
                    ?? throw new InvalidOperationException(
                        $"Registered query '{message.RegisteredQueryName}' not found for schema '{message.SchemaVersion}'");
                
                _logger.LogDebug("Using registered query: {QueryName}", message.RegisteredQueryName);
            }
            else if (!string.IsNullOrEmpty(message.SqlQuery))
            {
                sql = message.SqlQuery;
                _logger.LogDebug("Using custom SQL query");
                
                // Basic security validation
                ValidateSqlQuery(sql);
            }
            else
            {
                throw new InvalidOperationException("Either RegisteredQueryName or SqlQuery must be specified");
            }

            if (_options.EnableQueryLogging)
            {
                _logger.LogDebug("Executing SQL: {Sql} with {ParamCount} parameters",
                    sql,
                    message.Parameters?.Count ?? 0);
            }

            // Execute query
            var timeout = message.TimeoutSeconds ?? _options.DefaultTimeoutSeconds;
            string? resultJson;
            int rowCount;

            await using (var connection = new SqlConnection(_options.ConnectionString))
            {
                await connection.OpenAsync();

                if (message.ReturnMultipleRows)
                {
                    var results = await connection.QueryAsync<dynamic>(
                        sql,
                        message.Parameters,
                        commandTimeout: timeout);

                    var resultList = results.ToList();
                    rowCount = resultList.Count;
                    resultJson = JsonSerializer.Serialize(resultList);
                }
                else
                {
                    var result = await connection.QuerySingleOrDefaultAsync<dynamic>(
                        sql,
                        message.Parameters,
                        commandTimeout: timeout);

                    rowCount = result != null ? 1 : 0;
                    resultJson = result != null ? JsonSerializer.Serialize(result) : null;
                }
            }

            sw.Stop();

            // Send success response
            var response = new DynamicSqlQueryResponse
            {
                Success = true,
                ResultJson = resultJson,
                RowCount = rowCount,
                ExecutionTimeMs = sw.ElapsedMilliseconds,
                ExecutedSql = _options.EnableQueryLogging ? sql : null,
                CorrelationId = message.CorrelationId,
                ExecutedAt = DateTimeOffset.UtcNow
            };

            await _bus.Reply(response);

            _logger.LogInformation(
                "DynamicSqlQuery completed: {RowCount} rows in {ExecutionTime}ms",
                rowCount,
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            
            _logger.LogError(ex,
                "Error processing DynamicSqlQuery for Client: {ClientId}",
                message.ClientId);

            var response = new DynamicSqlQueryResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                ExecutionTimeMs = sw.ElapsedMilliseconds,
                CorrelationId = message.CorrelationId,
                ExecutedAt = DateTimeOffset.UtcNow
            };

            await _bus.Reply(response);
        }
    }

    private void ValidateSqlQuery(string sql)
    {
        var sqlLower = sql.ToLowerInvariant().Trim();

        // Block dangerous operations
        var dangerousKeywords = new[]
        {
            "drop ",
            "truncate ",
            "delete ",
            "insert ",
            "update ",
            "alter ",
            "create ",
            "exec ",
            "execute ",
            "xp_",
            "sp_",
            "grant ",
            "revoke ",
            ";--",
            "/*",
            "*/"
        };

        foreach (var keyword in dangerousKeywords)
        {
            if (sqlLower.Contains(keyword))
            {
                _logger.LogWarning("Blocked SQL query containing dangerous keyword: {Keyword}", keyword.Trim());
                throw new InvalidOperationException(
                    $"SQL query contains forbidden operation: {keyword.Trim()}. Only SELECT queries are allowed.");
            }
        }

        // Ensure it starts with SELECT
        if (!sqlLower.StartsWith("select ", StringComparison.Ordinal) &&
            !sqlLower.StartsWith("with ", StringComparison.Ordinal)) // Allow CTEs
        {
            _logger.LogWarning("Blocked non-SELECT query");
            throw new InvalidOperationException("Only SELECT queries are allowed");
        }

        _logger.LogDebug("SQL query validation passed");
    }
}
