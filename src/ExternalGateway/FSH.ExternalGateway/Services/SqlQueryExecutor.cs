using Dapper;
using FSH.ExternalGateway.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Data;
using System.Diagnostics;

namespace FSH.ExternalGateway.Services;

public interface ISqlQueryExecutor
{
    Task<T?> QuerySingleAsync<T>(string queryName, object? parameters = null, int? timeoutSeconds = null);
    Task<IEnumerable<T>> QueryAsync<T>(string queryName, object? parameters = null, int? timeoutSeconds = null);
}

public sealed class SqlQueryExecutor : ISqlQueryExecutor
{
    private readonly GatewayOptions _options;
    private readonly IQueryRepository _queryRepository;
    private readonly ILogger<SqlQueryExecutor> _logger;

    public SqlQueryExecutor(
        IOptions<GatewayOptions> options,
        IQueryRepository queryRepository,
        ILogger<SqlQueryExecutor> logger)
    {
        _options = options.Value;
        _queryRepository = queryRepository;
        _logger = logger;
    }

    public async Task<T?> QuerySingleAsync<T>(
        string queryName,
        object? parameters = null,
        int? timeoutSeconds = null)
    {
        var sql = GetSql(queryName);
        var timeout = timeoutSeconds ?? _options.DefaultTimeoutSeconds;

        using var connection = new SqlConnection(_options.ConnectionString);
        var sw = Stopwatch.StartNew();

        try
        {
            var result = await connection.QuerySingleOrDefaultAsync<T>(
                sql,
                parameters,
                commandTimeout: timeout,
                commandType: CommandType.Text);

            sw.Stop();
            LogQueryExecution(queryName, sw.ElapsedMilliseconds, success: true);

            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            LogQueryExecution(queryName, sw.ElapsedMilliseconds, success: false);
            _logger.LogError(ex, "Error executing query {QueryName}", queryName);
            throw;
        }
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(
        string queryName,
        object? parameters = null,
        int? timeoutSeconds = null)
    {
        var sql = GetSql(queryName);
        var timeout = timeoutSeconds ?? _options.DefaultTimeoutSeconds;

        using var connection = new SqlConnection(_options.ConnectionString);
        var sw = Stopwatch.StartNew();

        try
        {
            var result = await connection.QueryAsync<T>(
                sql,
                parameters,
                commandTimeout: timeout,
                commandType: CommandType.Text);

            sw.Stop();
            LogQueryExecution(queryName, sw.ElapsedMilliseconds, success: true, rowCount: result.Count());

            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            LogQueryExecution(queryName, sw.ElapsedMilliseconds, success: false);
            _logger.LogError(ex, "Error executing query {QueryName}", queryName);
            throw;
        }
    }

    private string GetSql(string queryName)
    {
        var sql = _queryRepository.GetQuery(queryName, _options.SchemaVersion);
        if (string.IsNullOrEmpty(sql))
        {
            throw new InvalidOperationException(
                $"Query '{queryName}' not found for schema version '{_options.SchemaVersion}'");
        }

        if (_options.EnableQueryLogging)
        {
            _logger.LogDebug("Executing query {QueryName} for schema {SchemaVersion}: {Sql}",
                queryName, _options.SchemaVersion, sql);
        }

        return sql;
    }

    private void LogQueryExecution(string queryName, long milliseconds, bool success, int? rowCount = null)
    {
        if (success)
        {
            var rowInfo = rowCount.HasValue ? $"{rowCount.Value} rows in " : "";
            _logger.LogInformation(
                "Query {QueryName} completed: {RowInfo}{ExecutionTime}ms",
                queryName, rowInfo, milliseconds);
        }
        else
        {
            _logger.LogWarning(
                "Query {QueryName} failed after {ExecutionTime}ms",
                queryName, milliseconds);
        }
    }
}
