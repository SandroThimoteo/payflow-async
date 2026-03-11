using Microsoft.EntityFrameworkCore;
using Payment.Application.Abstractions;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository, IUnitOfWork
{
    private readonly PaymentDbContext _context;

    public PaymentRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Payment.Domain.Entities.Payment payment)
    {
        await _context.Payments.AddAsync(payment);
    }

    public async Task<Payment.Domain.Entities.Payment?> GetByIdAsync(Guid id)
    {
        return await _context.Payments.FindAsync(id);
    }

    public async Task<Payment.Domain.Entities.Payment?> GetByExternalReferenceAsync(string externalReference)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p => p.ExternalReference == externalReference);
    }

    public Task UpdateAsync(Payment.Domain.Entities.Payment payment)
    {
        _context.Payments.Update(payment);
        return Task.CompletedTask;
    }

    public async Task CommitAsync()
    {
        await _context.SaveChangesAsync();
    }
}
