using FSH.ExternalGateway.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace FSH.ExternalGateway.Services;

/// <summary>
/// Service to initialize the SQL Server database with schema and seed data.
/// </summary>
public interface IDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

public sealed class DatabaseInitializer : IDatabaseInitializer
{
    private readonly GatewayOptions _options;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        IOptions<GatewayOptions> options,
        ILogger<DatabaseInitializer> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_options.ConnectionString))
        {
            _logger.LogWarning("SQL ConnectionString is empty. Skipping database initialization.");
            return;
        }

        try
        {
            _logger.LogInformation("Starting database initialization...");

            // Check if database is accessible
            await using var connection = new SqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            _logger.LogInformation("Connected to SQL Server successfully");

            // Check if tables already exist
            var tablesExist = await CheckTablesExistAsync(connection, cancellationToken);

            if (tablesExist)
            {
                _logger.LogInformation("Database tables already exist. Skipping initialization.");
                var count = await GetInvoiceCountAsync(connection, cancellationToken);
                _logger.LogInformation("Database contains {InvoiceCount} invoices", count);
                return;
            }

            _logger.LogInformation("Tables not found. Running initialization script...");

            // Read and execute initialization script
            var scriptPath = Path.Combine(AppContext.BaseDirectory, "Scripts", "init-database.sql");
            
            if (!File.Exists(scriptPath))
            {
                _logger.LogWarning("Initialization script not found at {ScriptPath}. Trying embedded resource...", scriptPath);
                await ExecuteEmbeddedScriptAsync(connection, cancellationToken);
            }
            else
            {
                var script = await File.ReadAllTextAsync(scriptPath, cancellationToken);
                await ExecuteScriptAsync(connection, script, cancellationToken);
            }

            _logger.LogInformation("Database initialization completed successfully");

            // Verify data
            var finalCount = await GetInvoiceCountAsync(connection, cancellationToken);
            _logger.LogInformation("Database initialized with {InvoiceCount} invoices", finalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing database");
            throw;
        }
    }

    private static async Task<bool> CheckTablesExistAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string checkQuery = @"
            SELECT COUNT(*) 
            FROM INFORMATION_SCHEMA.TABLES 
            WHERE TABLE_NAME IN ('Invoices', 'InvoiceLines')";

        await using var command = new SqlCommand(checkQuery, connection);
        var count = (int)(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
        return count == 2;
    }

    private static async Task<int> GetInvoiceCountAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string countQuery = "SELECT COUNT(*) FROM Invoices";
        await using var command = new SqlCommand(countQuery, connection);
        return (int)(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
    }

    private async Task ExecuteScriptAsync(SqlConnection connection, string script, CancellationToken cancellationToken)
    {
        // Execute the entire script as one batch since PostgreSQL supports multiple statements
        try
        {
            await using var command = new SqlCommand(script, connection);
            command.CommandTimeout = 60; // Increase timeout for schema creation
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing SQL script");
            throw;
        }
    }

    private async Task ExecuteEmbeddedScriptAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "FSH.ExternalGateway.Host.Scripts.init-database.sql";

        await using var stream = assembly.GetManifestResourceStream(resourceName);
        
        if (stream == null)
        {
            throw new InvalidOperationException($"Embedded resource '{resourceName}' not found");
        }

        using var reader = new StreamReader(stream);
        var script = await reader.ReadToEndAsync(cancellationToken);
        await ExecuteScriptAsync(connection, script, cancellationToken);
    }
}
