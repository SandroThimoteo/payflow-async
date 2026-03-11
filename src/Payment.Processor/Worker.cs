using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Payment.Application.Abstractions;
using Payment.Domain.Enums;

namespace Payment.Processor;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAmazonSQS _sqsClient;
    private readonly IConfiguration _configuration;

    // Quantas mensagens busca por vez (máx 10 no SQS)
    private const int MaxNumberOfMessages = 10;
    // Quanto tempo a mensagem fica invisível para outros consumers enquanto processamos
    private const int VisibilityTimeoutSeconds = 30;
    // Long polling: aguarda até 20s por mensagens antes de retornar vazio
    private const int WaitTimeSeconds = 20;

    public Worker(
        ILogger<Worker> logger,
        IServiceScopeFactory scopeFactory,
        IAmazonSQS sqsClient,
        IConfiguration configuration)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _sqsClient = sqsClient;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueName = _configuration["Aws:PaymentQueueName"] ?? "payflow-payments";
        var queueUrl = (await _sqsClient.GetQueueUrlAsync(queueName, stoppingToken)).QueueUrl;

        _logger.LogInformation("Payment Processor started. Listening on queue: {Queue}", queueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var response = await _sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = queueUrl,
                    MaxNumberOfMessages = MaxNumberOfMessages,
                    VisibilityTimeout = VisibilityTimeoutSeconds,
                    WaitTimeSeconds = WaitTimeSeconds,
                    AttributeNames = new List<string> { "ApproximateReceiveCount" }
                }, stoppingToken);

                if (response.Messages.Count == 0)
                    continue;

                _logger.LogInformation("Received {Count} message(s) from SQS.", response.Messages.Count);

                // Processa cada mensagem em paralelo
                var tasks = response.Messages.Select(msg =>
                    ProcessMessageAsync(msg, queueUrl, stoppingToken));

                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                // Shutdown limpo
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Worker loop. Retrying in 5s.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("Payment Processor stopped.");
    }

    private async Task ProcessMessageAsync(Message message, string queueUrl, CancellationToken stoppingToken)
    {
        Guid paymentId = Guid.Empty;

        try
        {
            // Deserializa o corpo: { "paymentId": "..." }
            var payload = JsonSerializer.Deserialize<PaymentMessage>(message.Body);
            if (payload is null || payload.paymentId == Guid.Empty)
            {
                _logger.LogWarning("Invalid message body, skipping. MessageId: {MessageId}", message.MessageId);
                await DeleteMessageAsync(queueUrl, message.ReceiptHandle);
                return;
            }

            paymentId = payload.paymentId;

            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var payment = await repository.GetByIdAsync(paymentId);
            if (payment is null)
            {
                _logger.LogWarning("Payment {PaymentId} not found. Discarding message.", paymentId);
                await DeleteMessageAsync(queueUrl, message.ReceiptHandle);
                return;
            }

            // Idempotência: ignora se já foi processado
            if (payment.Status != PaymentStatus.Pending)
            {
                _logger.LogInformation(
                    "Payment {PaymentId} is already in status {Status}. Skipping.",
                    paymentId, payment.Status);
                await DeleteMessageAsync(queueUrl, message.ReceiptHandle);
                return;
            }

            // Marca como Processing
            payment.MarkAsProcessing();
            await repository.UpdateAsync(payment);
            await unitOfWork.CommitAsync();

            // Chama o gateway externo (simulado)
            var gatewayResult = await CallGatewayAsync(payment, stoppingToken);

            // Aplica resultado
            if (gatewayResult == GatewayResult.Approved)
            {
                payment.MarkAsApproved();
                _logger.LogInformation("Payment {PaymentId} APPROVED.", paymentId);
            }
            else if (gatewayResult == GatewayResult.Rejected)
            {
                payment.MarkAsRejected();
                _logger.LogInformation("Payment {PaymentId} REJECTED by gateway.", paymentId);
            }
            else
            {
                // GatewayResult.Failed — não é retentável, vai p/ Failed
                payment.MarkAsFailed();
                _logger.LogWarning("Payment {PaymentId} FAILED (non-retryable gateway error).", paymentId);
            }

            await repository.UpdateAsync(payment);
            await unitOfWork.CommitAsync();

            // Só deleta da fila após persistir com sucesso
            await DeleteMessageAsync(queueUrl, message.ReceiptHandle);
        }
        catch (Exception ex)
        {
            // NÃO deleta: o SQS vai re-entregar após VisibilityTimeout
            // Após N tentativas (configurado na DLQ), vai automaticamente para a Dead Letter Queue
            _logger.LogError(ex,
                "Failed to process message {MessageId} for PaymentId {PaymentId}. Will retry.",
                message.MessageId, paymentId);
        }
    }

    // Simula chamada ao gateway externo (substituir pela integração real)
    private static async Task<GatewayResult> CallGatewayAsync(
        Payment.Domain.Entities.Payment payment,
        CancellationToken stoppingToken)
    {
        // Simula latência do gateway
        await Task.Delay(Random.Shared.Next(100, 500), stoppingToken);

        // Simulação: 80% aprovado, 10% rejeitado, 10% falha
        return Random.Shared.Next(100) switch
        {
            < 80 => GatewayResult.Approved,
            < 90 => GatewayResult.Rejected,
            _ => GatewayResult.Failed
        };
    }

    private async Task DeleteMessageAsync(string queueUrl, string receiptHandle)
    {
        await _sqsClient.DeleteMessageAsync(queueUrl, receiptHandle);
    }

    // Record para deserializar o body da mensagem SQS
    private record PaymentMessage(Guid paymentId);

    private enum GatewayResult { Approved, Rejected, Failed }
}
