using FSH.ExternalGateway.Messages.v1;
using FSH.ExternalGateway.Services;
using Microsoft.Extensions.Logging;
using Rebus.Bus;
using Rebus.Handlers;

namespace FSH.ExternalGateway.Handlers;

public sealed class GetInvoiceHandler : IHandleMessages<GetInvoiceRequest>
{
    private readonly ISqlQueryExecutor _sqlExecutor;
    private readonly IBus _bus;
    private readonly ILogger<GetInvoiceHandler> _logger;

    public GetInvoiceHandler(
        ISqlQueryExecutor sqlExecutor,
        IBus bus,
        ILogger<GetInvoiceHandler> logger)
    {
        _sqlExecutor = sqlExecutor;
        _bus = bus;
        _logger = logger;
    }

    public async Task Handle(GetInvoiceRequest message)
    {
        _logger.LogInformation(
            "Processing GetInvoice request for InvoiceId: {InvoiceId}, Client: {ClientId}",
            message.InvoiceId, message.ClientId);

        try
        {
            // Get invoice header
            var invoice = await _sqlExecutor.QuerySingleAsync<InvoiceDto>(
                "GetInvoice",
                new { InvoiceId = message.InvoiceId },
                message.TimeoutSeconds);

            if (invoice == null)
            {
                await ReplyNotFound(message);
                return;
            }

            // Get invoice details if requested
            if (message.IncludeDetails)
            {
                var details = await _sqlExecutor.QueryAsync<InvoiceLineDto>(
                    "GetInvoiceDetails",
                    new { InvoiceId = message.InvoiceId },
                    message.TimeoutSeconds);

                invoice = invoice with { Lines = details.ToList() };
            }

            // Send response
            var response = new GetInvoiceResponse
            {
                Success = true,
                Invoice = invoice,
                CorrelationId = message.CorrelationId,
                ExecutedAt = DateTimeOffset.UtcNow
            };

            await _bus.Reply(response);

            _logger.LogInformation(
                "Successfully processed GetInvoice for {InvoiceId}, {LineCount} lines in {ExecutionTime}ms",
                message.InvoiceId,
                invoice.Lines?.Count ?? 0,
                (DateTimeOffset.UtcNow - message.RequestedAt).TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing GetInvoice for InvoiceId: {InvoiceId}",
                message.InvoiceId);

            var response = new GetInvoiceResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                CorrelationId = message.CorrelationId,
                ExecutedAt = DateTimeOffset.UtcNow
            };

            await _bus.Reply(response);
        }
    }

    private async Task ReplyNotFound(GetInvoiceRequest message)
    {
        _logger.LogWarning(
            "Invoice not found: {InvoiceId}",
            message.InvoiceId);

        var response = new GetInvoiceResponse
        {
            Success = false,
            ErrorMessage = $"Invoice '{message.InvoiceId}' not found",
            CorrelationId = message.CorrelationId,
            ExecutedAt = DateTimeOffset.UtcNow
        };

        await _bus.Reply(response);
    }
}
