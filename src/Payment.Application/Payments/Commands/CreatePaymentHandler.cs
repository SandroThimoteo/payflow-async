using System.Text.Json;
using Payment.Application.Abstractions;
using Payment.Application.Payments.DTOs;

namespace Payment.Application.Payments.Commands;

public class CreatePaymentHandler
{
    private readonly IPaymentRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessagePublisher _publisher;
    private readonly IConfiguration _configuration;

    public CreatePaymentHandler(
        IPaymentRepository repository,
        IUnitOfWork unitOfWork,
        IMessagePublisher publisher,
        IConfiguration configuration)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _configuration = configuration;
    }

    public async Task<PaymentResponse> HandleAsync(CreatePaymentCommand command, CancellationToken cancellationToken = default)
    {
        // Idempotência: bloqueia ExternalReference duplicado
        var existing = await _repository.GetByExternalReferenceAsync(command.ExternalReference);
        if (existing is not null)
            throw new InvalidOperationException($"Payment with ExternalReference '{command.ExternalReference}' already exists.");

        var payment = new Payment.Domain.Entities.Payment(
            command.ExternalReference,
            command.CustomerId,
            command.Amount,
            command.Currency,
            command.Method
        );

        await _repository.AddAsync(payment);
        await _unitOfWork.CommitAsync();

        // Publica mensagem na fila SQS para o Processor consumir
        var queueName = _configuration["Aws:PaymentQueueName"] ?? "payflow-payments";
        var message = JsonSerializer.Serialize(new { paymentId = payment.Id });
        await _publisher.PublishAsync(queueName, message, cancellationToken);

        return new PaymentResponse(
            payment.Id,
            payment.ExternalReference,
            payment.CustomerId,
            payment.Amount,
            payment.Currency,
            payment.Method,
            payment.Status.ToString(),
            payment.CreatedAtUtc,
            payment.UpdatedAtUtc
        );
    }
}
