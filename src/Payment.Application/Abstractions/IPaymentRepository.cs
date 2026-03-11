namespace Payment.Application.Abstractions;

using Payment.Domain.Entities;

public interface IPaymentRepository
{
    Task AddAsync(Payment payment);
    Task<Payment?> GetByIdAsync(Guid id);
    Task<Payment?> GetByExternalReferenceAsync(string externalReference);
    Task UpdateAsync(Payment payment);
}

public interface IUnitOfWork
{
    Task CommitAsync();
}
