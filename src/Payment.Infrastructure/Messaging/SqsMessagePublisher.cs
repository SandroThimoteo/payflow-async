using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using Payment.Application.Abstractions;

namespace Payment.Infrastructure.Messaging;

public class SqsMessagePublisher : IMessagePublisher
{
    private readonly IAmazonSQS _sqsClient;
    private readonly ILogger<SqsMessagePublisher> _logger;

    public SqsMessagePublisher(IAmazonSQS sqsClient, ILogger<SqsMessagePublisher> logger)
    {
        _sqsClient = sqsClient;
        _logger = logger;
    }

    public async Task PublishAsync(string queueName, string messageBody, CancellationToken cancellationToken = default)
    {
        var queueUrlResponse = await _sqsClient.GetQueueUrlAsync(queueName, cancellationToken);

        var request = new SendMessageRequest
        {
            QueueUrl = queueUrlResponse.QueueUrl,
            MessageBody = messageBody
        };

        var response = await _sqsClient.SendMessageAsync(request, cancellationToken);

        _logger.LogInformation(
            "Message published to SQS. Queue: {Queue}, MessageId: {MessageId}",
            queueName, response.MessageId);
    }
}
