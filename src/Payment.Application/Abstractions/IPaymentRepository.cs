namespace Payment.Application.Abstractions;

using Payment.Domain.Entities;

public interface IPaymentRepository
{
    Task AddAsync(Payment payment);
    Task<Payment?> GetByIdAsync(Guid id);
    Task UpdateAsync(Payment payment);

    Task<IEnumerable<Payment>> GetByCustomerIdAsync(string customerId);
    Task SaveChangesAsync();
}

public interface IUnitOfWork
{
    Task CommitAsync();
}