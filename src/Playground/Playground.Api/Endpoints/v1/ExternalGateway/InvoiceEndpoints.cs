using Asp.Versioning;
using FSH.ExternalGateway.Messages.v1;
using FSH.Modules.Messaging.Contracts.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Playground.Api.Endpoints.v1.ExternalGateway;

public static class InvoiceEndpoints
{
    private sealed class LogCategory { }

    public static IEndpointRouteBuilder MapExternalGatewayInvoiceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var apiVersionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var group = endpoints
            .MapGroup("api/v{version:apiVersion}/external/invoices")
            .WithTags("External Gateway - Invoices")
            .WithApiVersionSet(apiVersionSet);

        group.MapGet("{clientId}/{invoiceId}", async (
            string clientId,
            string invoiceId,
            [FromQuery] bool includeDetails,
            [FromServices] IMessageBus messageBus,
            [FromServices] ILogger<LogCategory> logger) =>
        {
            logger.LogInformation(
                "Requesting invoice from external client {ClientId}: {InvoiceId}",
                clientId,
                invoiceId);

            try
            {
                // Determinar versión del esquema según el cliente
                var schemaVersion = GetSchemaVersionForClient(clientId);

                var request = new GetInvoiceRequest
                {
                    ClientId = clientId,
                    InvoiceId = invoiceId,
                    IncludeDetails = includeDetails,
                    SchemaVersion = schemaVersion,
                    CorrelationId = Guid.NewGuid().ToString()
                };

                // Enviar request al Gateway via RabbitMQ usando Pub/Sub
                await messageBus.PublishAsync(request);

                logger.LogInformation(
                    "Invoice request published for client {ClientId}, waiting for response...",
                    clientId);

                // NOTA: En producción, implementar suscripción a respuestas con CorrelationId
                // Por ahora retornamos accepted
                return Results.Accepted(
                    $"/api/v1/external/invoices/{clientId}/{invoiceId}/status",
                    new
                    {
                        Message = "Request sent to external gateway",
                        CorrelationId = request.CorrelationId,
                        ClientId = clientId,
                        InvoiceId = invoiceId
                    });
            }
            catch (TimeoutException ex)
            {
                logger.LogError(ex, "Timeout requesting invoice from client {ClientId}", clientId);
                return Results.Problem(
                    statusCode: StatusCodes.Status408RequestTimeout,
                    title: "Gateway Timeout",
                    detail: "The external gateway did not respond in time");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error requesting invoice from client {ClientId}", clientId);
                return Results.Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Gateway Error",
                    detail: ex.Message);
            }
        })
        .WithName("GetInvoiceFromExternalClient")
        .WithSummary("Obtiene una factura de un cliente remoto via Gateway")
        .Produces<GetInvoiceResponse>()
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status408RequestTimeout);

        return endpoints;
    }

    private static string GetSchemaVersionForClient(string clientId)
    {
        // TODO: Cargar desde configuración o base de datos
        return clientId.ToLowerInvariant() switch
        {
            "client1" => "v1",
            "client2" => "v2",
            "local-test" => "v1",
            _ => "v1"
        };
    }
}
