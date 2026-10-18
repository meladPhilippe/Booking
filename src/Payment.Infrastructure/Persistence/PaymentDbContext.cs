using Microsoft.EntityFrameworkCore;
using Payment.Domain.Entities;
using DomainPayment = Payment.Domain.Entities.Payment;

namespace Payment.Infrastructure.Persistence;

public sealed class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
{
    public DbSet<DomainPayment> Payments => Set<DomainPayment>();
    public DbSet<IdempotencyOperation> IdempotencyOperations => Set<IdempotencyOperation>();
    public DbSet<ProviderWebhookEvent> ProviderWebhookEvents => Set<ProviderWebhookEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var payment = modelBuilder.Entity<DomainPayment>();
        payment.ToTable("Payments");
        payment.HasKey(x => x.Id);
        payment.Property(x => x.MerchantId).HasMaxLength(128).IsRequired();
        payment.Property(x => x.MerchantReference).HasMaxLength(256).IsRequired();
        payment.Property(x => x.Provider).HasMaxLength(64).IsRequired();
        payment.Property(x => x.ProviderReference).HasMaxLength(256);
        payment.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        payment.Property(x => x.Method).HasConversion<string>().HasMaxLength(32).IsRequired();
        payment.Property(x => x.RowVersion).IsRowVersion();
        payment.OwnsOne(x => x.Amount, money =>
        {
            money.Property(x => x.Value).HasColumnName("Amount").HasColumnType("decimal(19,4)");
            money.Property(x => x.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
        });
        payment.HasIndex(x => new { x.MerchantId, x.MerchantReference });
        payment.HasIndex(x => new { x.MerchantId, x.Id });

        var idempotency = modelBuilder.Entity<IdempotencyOperation>();
        idempotency.ToTable("IdempotencyOperations");
        idempotency.HasKey(x => x.Id);
        idempotency.Property(x => x.MerchantId).HasMaxLength(128).IsRequired();
        idempotency.Property(x => x.Operation).HasMaxLength(64).IsRequired();
        idempotency.Property(x => x.Key).HasColumnName("IdempotencyKey").HasMaxLength(128).IsRequired();
        idempotency.Property(x => x.Fingerprint).HasMaxLength(64).IsRequired();
        idempotency.Property(x => x.RowVersion).IsRowVersion();
        idempotency.HasIndex(x => new { x.MerchantId, x.Operation, x.Key }).IsUnique();
        idempotency.HasIndex(x => new { x.MerchantId, x.PaymentId });
        idempotency.HasIndex(x => x.PaymentId);
        idempotency.HasOne<DomainPayment>().WithMany().HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Restrict);

        var webhook = modelBuilder.Entity<ProviderWebhookEvent>();
        webhook.ToTable("ProviderWebhookEvents");
        webhook.HasKey(x => x.Id);
        webhook.Property(x => x.Provider).HasMaxLength(64).IsRequired();
        webhook.Property(x => x.ExternalEventId).HasMaxLength(256).IsRequired();
        webhook.Property(x => x.EventType).HasMaxLength(128).IsRequired();
        webhook.Property(x => x.RelatedObjectId).HasMaxLength(256).IsRequired();
        webhook.Property(x => x.RowVersion).IsRowVersion();
        webhook.HasIndex(x => new { x.Provider, x.ExternalEventId }).IsUnique();
    }
}
