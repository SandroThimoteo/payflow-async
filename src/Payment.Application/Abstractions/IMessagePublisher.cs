namespace Payment.Application.Abstractions;

public interface IMessagePublisher
{
    Task PublishAsync(string queueName, string messageBody, CancellationToken cancellationToken = default);
}
