namespace FSH.ExternalGateway.Messages.v1;

public sealed record GetInvoiceRequest : SqlQueryRequest
{
    public string InvoiceId { get; init; } = string.Empty;
    public bool IncludeDetails { get; init; } = true;
}

public sealed record GetInvoiceResponse : SqlQueryResponse
{
    public InvoiceDto? Invoice { get; init; }
}

public sealed record InvoiceDto
{
    public string InvoiceId { get; init; } = string.Empty;
    public string InvoiceNumber { get; init; } = string.Empty;
    public DateTime InvoiceDate { get; init; }
    public decimal TotalAmount { get; init; }
    public string CustomerId { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public List<InvoiceLineDto>? Lines { get; init; }
}

public sealed record InvoiceLineDto
{
    public string LineId { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TotalPrice { get; init; }
}
