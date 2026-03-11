using FluentAssertions;
using Moq;
using Payment.Application.Abstractions;
using Payment.Application.Payments.Commands;
using Payment.Domain.Exceptions;

namespace Payment.Tests.Application;

public class CreatePaymentHandlerTests
{
    // ─── Mocks e Handler ────────────────────────────────────────────────────

    private readonly Mock<IPaymentRepository> _repositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IMessagePublisher> _publisherMock = new();

    private CreatePaymentHandler CreateHandler() =>
        new(_repositoryMock.Object, _unitOfWorkMock.Object, _publisherMock.Object);

    private static CreatePaymentCommand ValidCommand(string externalReference = "ORDER-001") =>
        new(externalReference, "CLIENT-123", 150.00m, "BRL", "CREDIT_CARD");

    // ─── Happy path ─────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_WithValidCommand_ShouldReturnPaymentResponseWithPendingStatus()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetByExternalReferenceAsync(It.IsAny<string>()))
            .ReturnsAsync((Payment.Domain.Entities.Payment?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(ValidCommand());

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Pending");
        result.ExternalReference.Should().Be("ORDER-001");
        result.Amount.Should().Be(150.00m);
    }

    [Fact]
    public async Task HandleAsync_WithValidCommand_ShouldPersistAndPublishMessage()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetByExternalReferenceAsync(It.IsAny<string>()))
            .ReturnsAsync((Payment.Domain.Entities.Payment?)null);

        var handler = CreateHandler();

        // Act
        await handler.HandleAsync(ValidCommand());

        // Assert — garante que salvou no banco E publicou na fila
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Payment.Domain.Entities.Payment>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
        _publisherMock.Verify(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── Idempotência ────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_WhenExternalReferenceAlreadyExists_ShouldThrowInvalidOperationException()
    {
        // Arrange — simula pagamento já existente no banco
        var existingPayment = new Payment.Domain.Entities.Payment("ORDER-001", "CLIENT-123", 150m, "BRL", "CREDIT_CARD");

        _repositoryMock
            .Setup(r => r.GetByExternalReferenceAsync("ORDER-001"))
            .ReturnsAsync(existingPayment);

        var handler = CreateHandler();

        // Act
        var act = () => handler.HandleAsync(ValidCommand("ORDER-001"));

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ORDER-001*already exists*");
    }

    [Fact]
    public async Task HandleAsync_WhenExternalReferenceAlreadyExists_ShouldNotSaveOrPublish()
    {
        // Arrange
        var existingPayment = new Payment.Domain.Entities.Payment("ORDER-001", "CLIENT-123", 150m, "BRL", "CREDIT_CARD");

        _repositoryMock
            .Setup(r => r.GetByExternalReferenceAsync("ORDER-001"))
            .ReturnsAsync(existingPayment);

        var handler = CreateHandler();

        // Act
        try { await handler.HandleAsync(ValidCommand("ORDER-001")); } catch { }

        // Assert — nada deve ter sido salvo ou publicado
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Payment.Domain.Entities.Payment>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        _publisherMock.Verify(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── Validações de domínio ───────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_WithZeroAmount_ShouldThrowDomainException()
    {
        _repositoryMock
            .Setup(r => r.GetByExternalReferenceAsync(It.IsAny<string>()))
            .ReturnsAsync((Payment.Domain.Entities.Payment?)null);

        var handler = CreateHandler();
        var command = new CreatePaymentCommand("ORDER-001", "CLIENT-123", 0m, "BRL", "CREDIT_CARD");

        var act = () => handler.HandleAsync(command);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Amount*");
    }
}
