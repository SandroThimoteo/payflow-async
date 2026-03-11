namespace Payment.Application.Payments.Commands;

public record CreatePaymentCommand(
    string ExternalReference,
    string CustomerId,
    decimal Amount,
    string Currency,
    string Method
);
