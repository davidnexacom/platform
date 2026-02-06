namespace FSH.ExternalGateway.Messages;

public abstract record SqlQueryRequest
{
    public string ClientId { get; init; } = string.Empty;
    public string SchemaVersion { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString();
    public DateTimeOffset RequestedAt { get; init; } = DateTimeOffset.UtcNow;
    public int? TimeoutSeconds { get; init; }
}

public abstract record SqlQueryResponse
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
    public DateTimeOffset ExecutedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Dynamic SQL query request - SQL and parameters are sent from the caller.
/// Gateway executes the query and returns results as JSON.
/// </summary>
public sealed record DynamicSqlQueryRequest : SqlQueryRequest
{
    /// <summary>
    /// SQL query to execute (must be parameterized for security).
    /// Example: "SELECT * FROM invoices WHERE invoiceid = @InvoiceId"
    /// </summary>
    public string SqlQuery { get; init; } = string.Empty;
    
    /// <summary>
    /// Parameters for the SQL query (prevents SQL injection).
    /// Key = parameter name (without @), Value = parameter value.
    /// Example: { "InvoiceId", "INV-001" }
    /// </summary>
    public Dictionary<string, object?>? Parameters { get; init; }
    
    /// <summary>
    /// Whether to return multiple rows or single row.
    /// True = QueryAsync (returns array), False = QuerySingleAsync (returns single object).
    /// </summary>
    public bool ReturnMultipleRows { get; init; } = true;
    
    /// <summary>
    /// Optional: Pre-registered query name from QueryRepository.
    /// If specified, SqlQuery is ignored and the registered query is used instead.
    /// This is useful for frequently-used queries that are validated and optimized.
    /// </summary>
    public string? RegisteredQueryName { get; init; }
    
    /// <summary>
    /// Maximum execution time in seconds. Overrides gateway default timeout.
    /// </summary>
    public new int? TimeoutSeconds { get; init; }
}

/// <summary>
/// Dynamic SQL query response with flexible data structure.
/// Result is returned as JSON string for maximum flexibility.
/// </summary>
public sealed record DynamicSqlQueryResponse : SqlQueryResponse
{
    /// <summary>
    /// Result data as JSON string.
    /// Single object: {"invoiceid":"INV-001","amount":100.00}
    /// Multiple objects: [{"invoiceid":"INV-001",...},{"invoiceid":"INV-002",...}]
    /// Can be deserialized to any type on the client side.
    /// </summary>
    public string? ResultJson { get; init; }
    
    /// <summary>
    /// Number of rows/objects returned.
    /// </summary>
    public int RowCount { get; init; }
    
    /// <summary>
    /// Execution time in milliseconds.
    /// </summary>
    public long ExecutionTimeMs { get; init; }
    
    /// <summary>
    /// SQL query that was executed (for debugging/logging).
    /// </summary>
    public string? ExecutedSql { get; init; }
}
