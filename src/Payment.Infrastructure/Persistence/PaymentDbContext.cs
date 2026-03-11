using Microsoft.EntityFrameworkCore;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Persistence;

public class PaymentDbContext : DbContext
{
    public DbSet<Payment.Domain.Entities.Payment> Payments => Set<Payment.Domain.Entities.Payment>();

    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment.Domain.Entities.Payment>(entity =>
        {
            entity.ToTable("payments");

            entity.HasKey(p => p.Id);

            entity.Property(p => p.Id)
                  .HasColumnName("id");

            entity.Property(p => p.ExternalReference)
                  .HasColumnName("external_reference")
                  .HasMaxLength(100)
                  .IsRequired();

            entity.HasIndex(p => p.ExternalReference)
                  .IsUnique();

            entity.Property(p => p.CustomerId)
                  .HasColumnName("customer_id")
                  .HasMaxLength(100)
                  .IsRequired();

            entity.Property(p => p.Amount)
                  .HasColumnName("amount")
                  .HasColumnType("decimal(18,2)")
                  .IsRequired();

            entity.Property(p => p.Currency)
                  .HasColumnName("currency")
                  .HasMaxLength(3)
                  .IsRequired();

            entity.Property(p => p.Method)
                  .HasColumnName("method")
                  .HasMaxLength(30)
                  .IsRequired();

            entity.Property(p => p.Status)
                  .HasColumnName("status")
                  .HasMaxLength(20)
                  .HasConversion<string>()
                  .IsRequired();

            entity.Property(p => p.CreatedAtUtc)
                  .HasColumnName("created_at_utc")
                  .IsRequired();

            entity.Property(p => p.UpdatedAtUtc)
                  .HasColumnName("updated_at_utc");
        });
    }
}
