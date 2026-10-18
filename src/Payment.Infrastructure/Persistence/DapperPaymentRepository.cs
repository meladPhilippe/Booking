using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Payment.Application.Abstractions;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Domain.ValueObjects;
using DomainPayment = Payment.Domain.Entities.Payment;

namespace Payment.Infrastructure.Persistence;

public sealed class DapperPaymentRepository(string connectionString) : IPaymentRepository
{
    public async Task<IdempotentCreateResult> ClaimOperationAsync(IdempotencyOperation operation, CancellationToken cancellationToken)
    {
        const string sql = "INSERT INTO IdempotencyOperations (Id, MerchantId, Operation, IdempotencyKey, FingerprintVersion, Fingerprint, PaymentId, CreatedAt, UpdatedAt) VALUES (@Id, @MerchantId, @Operation, @Key, @FingerprintVersion, @Fingerprint, @PaymentId, @CreatedAt, @UpdatedAt);";
        await using var connection = new SqlConnection(connectionString);
        try { await connection.ExecuteAsync(new CommandDefinition(sql, operation, cancellationToken: cancellationToken)); return new IdempotentCreateResult(IdempotentCreateKind.Created); }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            const string existingSql = "SELECT Fingerprint FROM IdempotencyOperations WHERE MerchantId = @MerchantId AND Operation = @Operation AND IdempotencyKey = @Key;";
            var fingerprint = await connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(existingSql, operation, cancellationToken: cancellationToken));
            return fingerprint == operation.Fingerprint ? new IdempotentCreateResult(IdempotentCreateKind.Replay) : new IdempotentCreateResult(IdempotentCreateKind.FingerprintConflict);
        }
    }
    public async Task<IdempotentCreateResult> CreateOrGetAsync(DomainPayment payment, IdempotencyOperation operation, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO Payments (Id, MerchantId, MerchantReference, Amount, Currency, Method, Provider, ProviderReference, Status, CreatedAt, UpdatedAt)
            OUTPUT inserted.RowVersion
            VALUES (@Id, @MerchantId, @MerchantReference, @Amount, @Currency, @Method, @Provider, @ProviderReference, @Status, @CreatedAt, @UpdatedAt);
            """;
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var rowVersion = await connection.ExecuteScalarAsync<byte[]>(new CommandDefinition(sql, ToParameters(payment), transaction, cancellationToken: cancellationToken));
            if (rowVersion is null) throw new DBConcurrencyException("Payment insert did not return a concurrency version.");
            payment.SetRowVersion(rowVersion);
            const string operationSql = """
                INSERT INTO IdempotencyOperations (Id, MerchantId, Operation, IdempotencyKey, FingerprintVersion, Fingerprint, PaymentId, CreatedAt, UpdatedAt)
                OUTPUT inserted.RowVersion
                VALUES (@Id, @MerchantId, @Operation, @Key, @FingerprintVersion, @Fingerprint, @PaymentId, @CreatedAt, @UpdatedAt);
                """;
            var operationVersion = await connection.ExecuteScalarAsync<byte[]>(new CommandDefinition(operationSql, operation, transaction, cancellationToken: cancellationToken));
            if (operationVersion is null) throw new DBConcurrencyException("Idempotency operation insert did not return a concurrency version.");
            await transaction.CommitAsync(cancellationToken);
            return new IdempotentCreateResult(IdempotentCreateKind.Created, payment);
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            await transaction.RollbackAsync(cancellationToken);
            const string existingSql = """
                SELECT Id, MerchantId, Operation, IdempotencyKey AS [Key], FingerprintVersion, Fingerprint, PaymentId, CreatedAt, UpdatedAt, RowVersion
                FROM IdempotencyOperations
                WHERE MerchantId = @MerchantId AND Operation = @Operation AND IdempotencyKey = @Key;
                """;
            var existing = await connection.QuerySingleOrDefaultAsync<IdempotencyRow>(new CommandDefinition(existingSql, operation, cancellationToken: cancellationToken));
            if (existing is null) throw;
            if (existing.FingerprintVersion != operation.FingerprintVersion || existing.Fingerprint != operation.Fingerprint)
                return new IdempotentCreateResult(IdempotentCreateKind.FingerprintConflict);
            return new IdempotentCreateResult(IdempotentCreateKind.Replay, await GetAsync(operation.MerchantId, existing.PaymentId, cancellationToken));
        }
    }

    public async Task<DomainPayment?> GetAsync(string merchantId, Guid paymentId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT Id, MerchantId, MerchantReference, Amount, Currency, Method, Provider, ProviderReference, Status, CreatedAt, UpdatedAt, RowVersion FROM Payments WHERE Id = @paymentId AND MerchantId = @merchantId;";
        await using var connection = new SqlConnection(connectionString);
        var row = await connection.QuerySingleOrDefaultAsync<PaymentRow>(new CommandDefinition(sql, new { paymentId, merchantId }, cancellationToken: cancellationToken));
        return row is null ? null : ToPayment(row);
    }

    public async Task<DomainPayment?> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT Id, MerchantId, MerchantReference, Amount, Currency, Method, Provider, ProviderReference, Status, CreatedAt, UpdatedAt, RowVersion FROM Payments WHERE Id = @paymentId;";
        await using var connection = new SqlConnection(connectionString);
        var row = await connection.QuerySingleOrDefaultAsync<PaymentRow>(new CommandDefinition(sql, new { paymentId }, cancellationToken: cancellationToken));
        return row is null ? null : ToPayment(row);
    }

    public async Task<bool> TryRecordWebhookEventAsync(ProviderWebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        const string sql = "INSERT INTO ProviderWebhookEvents (Id, Provider, ExternalEventId, EventType, RelatedObjectId, ReceivedAt, ProcessedAt) VALUES (@Id, @Provider, @ExternalEventId, @EventType, @RelatedObjectId, @ReceivedAt, @ProcessedAt);";
        await using var connection = new SqlConnection(connectionString);
        try { await connection.ExecuteAsync(new CommandDefinition(sql, webhookEvent, cancellationToken: cancellationToken)); return true; }
        catch (SqlException exception) when (exception.Number is 2601 or 2627) { return false; }
    }

    public async Task UpdateAsync(DomainPayment payment, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE Payments SET ProviderReference = @ProviderReference, Status = @Status, UpdatedAt = @UpdatedAt
            OUTPUT inserted.RowVersion
            WHERE Id = @Id AND RowVersion = @RowVersion;
            """;
        await using var connection = new SqlConnection(connectionString);
        var rowVersion = await connection.ExecuteScalarAsync<byte[]>(new CommandDefinition(sql, ToParameters(payment), cancellationToken: cancellationToken));
        if (rowVersion is null) throw new DBConcurrencyException("Payment was changed by another operation.");
        payment.SetRowVersion(rowVersion);
    }

    private static object ToParameters(DomainPayment payment) => new
    {
        payment.Id, payment.MerchantId, payment.MerchantReference, Amount = payment.Amount.Value, Currency = payment.Amount.Currency,
        Method = payment.Method.ToString(), payment.Provider, payment.ProviderReference, Status = payment.Status.ToString(), payment.CreatedAt, payment.UpdatedAt, payment.RowVersion
    };

    private static DomainPayment ToPayment(PaymentRow row) => DomainPayment.Rehydrate(row.Id, row.MerchantId, row.MerchantReference, Money.Create(row.Amount, row.Currency, 4), Enum.Parse<PaymentMethod>(row.Method), row.Provider, row.ProviderReference, Enum.Parse<PaymentStatus>(row.Status), row.CreatedAt, row.UpdatedAt, row.RowVersion);

    private sealed class PaymentRow
    {
        public Guid Id { get; init; }
        public string MerchantId { get; init; } = string.Empty;
        public string MerchantReference { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public string Currency { get; init; } = string.Empty;
        public string Method { get; init; } = string.Empty;
        public string Provider { get; init; } = string.Empty;
        public string? ProviderReference { get; init; }
        public string Status { get; init; } = string.Empty;
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset UpdatedAt { get; init; }
        public byte[] RowVersion { get; init; } = [];
    }

    private sealed class IdempotencyRow
    {
        public int FingerprintVersion { get; init; }
        public string Fingerprint { get; init; } = string.Empty;
        public Guid PaymentId { get; init; }
    }
}
