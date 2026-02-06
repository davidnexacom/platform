using Asp.Versioning;
using FSH.ExternalGateway.Messages;
using FSH.Modules.Messaging.Contracts.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Playground.Api.Endpoints.v1.ExternalGateway;

public static class DynamicSqlQueryEndpoints
{
    public static IEndpointRouteBuilder MapDynamicSqlQueryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var apiVersionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var group = endpoints
            .MapGroup("api/v{version:apiVersion}/external/sql")
            .WithTags("External Gateway - Dynamic SQL")
            .WithApiVersionSet(apiVersionSet);

        group.MapPost("query", async (
            [FromBody] DynamicSqlQueryRequestDto request,
            [FromServices] IMessageBus messageBus,
            [FromServices] ILogger<LogCategory> logger) =>
        {
            logger.LogInformation(
                "Received dynamic SQL query request for client: {ClientId}",
                request.ClientId);

            try
            {
                var message = new DynamicSqlQueryRequest
                {
                    ClientId = request.ClientId,
                    SchemaVersion = request.SchemaVersion ?? "v1",
                    SqlQuery = request.SqlQuery ?? string.Empty,
                    Parameters = request.Parameters,
                    ReturnMultipleRows = request.ReturnMultipleRows,
                    RegisteredQueryName = request.RegisteredQueryName,
                    TimeoutSeconds = request.TimeoutSeconds,
                    CorrelationId = Guid.NewGuid().ToString()
                };

                // Send to gateway
                await messageBus.PublishAsync(message);

                logger.LogInformation(
                    "Dynamic SQL query request published for client {ClientId} with CorrelationId {CorrelationId}",
                    request.ClientId,
                    message.CorrelationId);

                return Results.Accepted(
                    $"/api/v1/external/sql/status/{message.CorrelationId}",
                    new
                    {
                        Message = "Query sent to external gateway",
                        CorrelationId = message.CorrelationId,
                        ClientId = request.ClientId
                    });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing dynamic SQL query request");
                return Results.Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Gateway Error",
                    detail: ex.Message);
            }
        })
        .WithName("ExecuteDynamicSqlQuery")
        .WithSummary("Execute a dynamic SQL query on external client database")
        .WithDescription(
            "Sends a SQL query to the external gateway for execution. " +
            "Query must be parameterized for security. " +
            "Results are returned as JSON.")
        .Produces(StatusCodes.Status202Accepted)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError);

        return endpoints;
    }

    private sealed class LogCategory { }
}

/// <summary>
/// DTO for dynamic SQL query requests from API clients.
/// </summary>
public sealed record DynamicSqlQueryRequestDto
{
    /// <summary>
    /// Client ID to route the query to the correct gateway.
    /// </summary>
    public required string ClientId { get; init; }

    /// <summary>
    /// Schema version of the target database.
    /// </summary>
    public string? SchemaVersion { get; init; }

    /// <summary>
    /// SQL query to execute (must be parameterized).
    /// Example: "SELECT * FROM invoices WHERE invoiceid = @InvoiceId AND customerid = @CustomerId"
    /// </summary>
    public string? SqlQuery { get; init; }

    /// <summary>
    /// Parameters for the SQL query.
    /// Example: { "InvoiceId": "INV-001", "CustomerId": "CUST-001" }
    /// </summary>
    public Dictionary<string, object?>? Parameters { get; init; }

    /// <summary>
    /// Whether to return multiple rows (true) or single row (false).
    /// Default: true
    /// </summary>
    public bool ReturnMultipleRows { get; init; } = true;

    /// <summary>
    /// Optional: Pre-registered query name from gateway's QueryRepository.
    /// If specified, SqlQuery is ignored.
    /// Example: "GetInvoice", "GetCustomer"
    /// </summary>
    public string? RegisteredQueryName { get; init; }

    /// <summary>
    /// Maximum execution time in seconds.
    /// </summary>
    public int? TimeoutSeconds { get; init; }
}
