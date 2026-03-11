namespace Payment.Application.Abstractions;

public interface IMessagePublisher
{
    Task PublishAssync(string queueName, string messageBody, CancellationToken cancellationToken = default);
}