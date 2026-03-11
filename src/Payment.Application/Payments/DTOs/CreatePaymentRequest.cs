namespace Payment.Application.Payments.DTOs;

public record CreatePaymentRequest(
    string ExternalReference,
    string CustomerId,
    decimal Amount,
    string Currency,
    string Method
);
