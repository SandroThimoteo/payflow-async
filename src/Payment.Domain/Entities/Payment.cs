using Payment.Domain.Exceptions; 
using Payment.Domain.Enums;
namespace Payment.Domain.Entities;

public class Payment
{

    public Guid Id { get; private set; }
    public string ExternalReference { get; private set; } = string.Empty;
    public string CustomerId { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "BRL";
    public string Method { get; private set; } = string.Empty;
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private Payment() { }

    public Payment( 
        string externalReference,
        string customerId,
        decimal amount,
        string currency,
        string method
        )
    {
        if (string.IsNullOrWhiteSpace(customerId))
            throw new DomainException("CustomerId cannot be null.");

        if (amount <= 0)
            throw new DomainException("Amount must be greater than zero.");

        if (string.IsNullOrWhiteSpace(externalReference))
            throw new DomainException("ExternalReference cannot be null.");

        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainException("Currency cannot be null.");

        if (string.IsNullOrWhiteSpace(method))
            throw new DomainException("Method cannot be null.");
        Id = Guid.NewGuid();
        ExternalReference = externalReference;
        CustomerId = customerId;
        Amount = amount;
        Currency = currency;
        Method = method;
        Status = PaymentStatus.Pending;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void MarkAsProcessing()
    {
        if (Status != PaymentStatus.Pending)
            throw new DomainException("Payment must be in PENDING status to be marked as PROCESSING.");
        Status = PaymentStatus.Processing;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkAsApproved()
    {
        if (Status != PaymentStatus.Processing)
            throw new DomainException("Payment must be in PROCESSING status to be approved.");
        Status = PaymentStatus.Approved;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkAsRejected()
    {
        if (Status != PaymentStatus.Processing)
            throw new DomainException("Payment must be in PROCESSING status to be rejected.");
        Status = PaymentStatus.Rejected;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkAsFailed()
    {
        if (Status != PaymentStatus.Processing)
            throw new DomainException("Payment must be in PROCESSING status to be failed.");
        Status = PaymentStatus.Failed;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}