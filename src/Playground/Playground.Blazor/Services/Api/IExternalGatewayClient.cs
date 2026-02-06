namespace FSH.Playground.Blazor.Services.Api;

/// <summary>
/// Client for External Gateway operations
/// </summary>
public interface IExternalGatewayClient
{
    Task<InvoiceResponse?> GetInvoiceAsync(string clientId, string invoiceId, bool includeDetails = true, CancellationToken cancellationToken = default);
}

public class ExternalGatewayClient : IExternalGatewayClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExternalGatewayClient> _logger;

    public ExternalGatewayClient(HttpClient httpClient, ILogger<ExternalGatewayClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<InvoiceResponse?> GetInvoiceAsync(
        string clientId,
        string invoiceId,
        bool includeDetails = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"api/v1/external/invoices/{clientId}/{invoiceId}?includeDetails={includeDetails}",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<InvoiceResponse>(cancellationToken);
            }

            _logger.LogWarning("Failed to get invoice. Status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoice {InvoiceId} from client {ClientId}", invoiceId, clientId);
            throw;
        }
    }
}

public record InvoiceResponse
{
    public string Message { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
    public string InvoiceId { get; init; } = string.Empty;
}

public record InvoiceDto
{
    public string InvoiceId { get; init; } = string.Empty;
    public string InvoiceNumber { get; init; } = string.Empty;
    public DateTime InvoiceDate { get; init; }
    public decimal TotalAmount { get; init; }
    public string CustomerId { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public List<InvoiceLineDto>? Lines { get; init; }
}

public record InvoiceLineDto
{
    public string LineId { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TotalPrice { get; init; }
}
