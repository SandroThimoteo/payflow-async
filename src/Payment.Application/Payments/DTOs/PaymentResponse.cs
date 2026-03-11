namespace Payment.Application.Payments.DTOs;

public record PaymentResponse(
    Guid Id,
    string ExternalReference,
    string CustomerId,
    decimal Amount,
    string Currency,
    string Method,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);
