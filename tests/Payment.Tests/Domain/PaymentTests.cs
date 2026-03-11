using FluentAssertions;
using Payment.Domain.Entities;
using Payment.Domain.Exceptions;

namespace Payment.Tests.Domain;

public class PaymentTests
{
    // ─── Helpers ────────────────────────────────────────────────────────────

    private static Payment.Domain.Entities.Payment CreateValidPayment(
        string externalReference = "ORDER-001",
        string customerId = "CLIENT-123",
        decimal amount = 150.00m,
        string currency = "BRL",
        string method = "CREDIT_CARD")
    {
        return new Payment.Domain.Entities.Payment(externalReference, customerId, amount, currency, method);
    }

    // ─── Constructor ────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithValidData_ShouldCreatePaymentWithPendingStatus()
    {
        // Arrange & Act
        var payment = CreateValidPayment();

        // Assert
        payment.Status.Should().Be(Payment.Domain.Enums.PaymentStatus.Pending);
        payment.Id.Should().NotBeEmpty();
        payment.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        payment.UpdatedAtUtc.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithInvalidExternalReference_ShouldThrowDomainException(string? externalReference)
    {
        // Act
        var act = () => CreateValidPayment(externalReference: externalReference!);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("*ExternalReference*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithInvalidCustomerId_ShouldThrowDomainException(string? customerId)
    {
        var act = () => CreateValidPayment(customerId: customerId!);

        act.Should().Throw<DomainException>()
           .WithMessage("*CustomerId*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public void Constructor_WithInvalidAmount_ShouldThrowDomainException(decimal amount)
    {
        var act = () => CreateValidPayment(amount: amount);

        act.Should().Throw<DomainException>()
           .WithMessage("*Amount*");
    }

    // ─── MarkAsProcessing ───────────────────────────────────────────────────

    [Fact]
    public void MarkAsProcessing_WhenPending_ShouldChangeStatusToProcessing()
    {
        var payment = CreateValidPayment();

        payment.MarkAsProcessing();

        payment.Status.Should().Be(Payment.Domain.Enums.PaymentStatus.Processing);
        payment.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsProcessing_WhenAlreadyProcessing_ShouldThrowDomainException()
    {
        var payment = CreateValidPayment();
        payment.MarkAsProcessing();

        var act = () => payment.MarkAsProcessing();

        act.Should().Throw<DomainException>()
           .WithMessage("*PENDING*");
    }

    // ─── MarkAsApproved ─────────────────────────────────────────────────────

    [Fact]
    public void MarkAsApproved_WhenProcessing_ShouldChangeStatusToApproved()
    {
        var payment = CreateValidPayment();
        payment.MarkAsProcessing();

        payment.MarkAsApproved();

        payment.Status.Should().Be(Payment.Domain.Enums.PaymentStatus.Approved);
        payment.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsApproved_WhenPending_ShouldThrowDomainException()
    {
        var payment = CreateValidPayment();

        var act = () => payment.MarkAsApproved();

        act.Should().Throw<DomainException>()
           .WithMessage("*PROCESSING*");
    }

    // ─── MarkAsRejected ─────────────────────────────────────────────────────

    [Fact]
    public void MarkAsRejected_WhenProcessing_ShouldChangeStatusToRejected()
    {
        var payment = CreateValidPayment();
        payment.MarkAsProcessing();

        payment.MarkAsRejected();

        payment.Status.Should().Be(Payment.Domain.Enums.PaymentStatus.Rejected);
        payment.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsRejected_WhenPending_ShouldThrowDomainException()
    {
        var payment = CreateValidPayment();

        var act = () => payment.MarkAsRejected();

        act.Should().Throw<DomainException>()
           .WithMessage("*PROCESSING*");
    }

    // ─── MarkAsFailed ───────────────────────────────────────────────────────

    [Fact]
    public void MarkAsFailed_WhenProcessing_ShouldChangeStatusToFailed()
    {
        var payment = CreateValidPayment();
        payment.MarkAsProcessing();

        payment.MarkAsFailed();

        payment.Status.Should().Be(Payment.Domain.Enums.PaymentStatus.Failed);
        payment.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsFailed_WhenPending_ShouldThrowDomainException()
    {
        var payment = CreateValidPayment();

        var act = () => payment.MarkAsFailed();

        act.Should().Throw<DomainException>()
           .WithMessage("*PROCESSING*");
    }
}
