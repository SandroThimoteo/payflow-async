namespace Payment.Domain.Entities;

public class Payment
{

    // Criação de uma entidade de pagamento com propriedades e métodos para gerenciar o estado do pagamento.
    public Guid Id { get; private set; }
    public string ExternalReference { get; private set; } = string.Empty;
    public string CustomerId { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "BRL";
    public string Method { get; private set; } = string.Empty;
    public string Status { get; private set; } = "PENDING";
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private Payment() { }

    public Payment(
        string externalReference,
        string customerId,
        decimal amount,
        string currency,
        string method)
    {
        Id = Guid.NewGuid();
        ExternalReference = externalReference;
        CustomerId = customerId;
        Amount = amount;
        Currency = currency;
        Method = method;
        Status = "PENDING";
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void MarkAsProcessing()
    {
        Status = "PROCESSING";
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkAsApproved()
    {
        Status = "APPROVED";
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkAsRejected()
    {
        Status = "REJECTED";
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkAsFailed()
    {
        Status = "FAILED";
        UpdatedAtUtc = DateTime.UtcNow;
    }
}