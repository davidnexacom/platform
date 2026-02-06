namespace FSH.ExternalGateway.Services;

public interface IQueryRepository
{
    string? GetQuery(string queryName, string schemaVersion);
    void RegisterQuery(string queryName, string schemaVersion, string sql);
}

public sealed class QueryRepository : IQueryRepository
{
    private readonly Dictionary<string, Dictionary<string, string>> _queries = new();

    public QueryRepository()
    {
        RegisterDefaultQueries();
    }

    public string? GetQuery(string queryName, string schemaVersion)
    {
        if (_queries.TryGetValue(queryName, out var versions))
        {
            if (versions.TryGetValue(schemaVersion, out var sql))
            {
                return sql;
            }
        }
        return null;
    }

    public void RegisterQuery(string queryName, string schemaVersion, string sql)
    {
        if (!_queries.ContainsKey(queryName))
        {
            _queries[queryName] = new Dictionary<string, string>();
        }
        _queries[queryName][schemaVersion] = sql;
    }

    private void RegisterDefaultQueries()
    {
        // GetInvoice - Schema v1 (SQL Server - matches init-database.sql schema)
        RegisterQuery(
            "GetInvoice",
            "v1",
            """
            SELECT 
                InvoiceId,
                InvoiceNumber,
                InvoiceDate,
                TotalAmount,
                CustomerId,
                CustomerName
            FROM Invoices
            WHERE InvoiceId = @InvoiceId
            """);

        RegisterQuery(
            "GetInvoiceDetails",
            "v1",
            """
            SELECT 
                LineId,
                Description,
                Quantity,
                UnitPrice,
                TotalPrice
            FROM InvoiceLines
            WHERE InvoiceId = @InvoiceId
            ORDER BY LineId
            """);
    }
}
