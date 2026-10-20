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
        const string sql = "INSERT INTO IdempotencyOperations (Id, MerchantId, Operation, IdempotencyKey, FingerprintVersion, Fingerprint, PaymentId, RefundId, CreatedAt, UpdatedAt) VALUES (@Id, @MerchantId, @Operation, @Key, @FingerprintVersion, @Fingerprint, @PaymentId, @RefundId, @CreatedAt, @UpdatedAt);";
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
            INSERT INTO Payments (Id, MerchantId, MerchantReference, Amount, Currency, Method, Provider, ProviderReference, ProviderPaymentReference, Status, CreatedAt, UpdatedAt)
            OUTPUT inserted.RowVersion
            VALUES (@Id, @MerchantId, @MerchantReference, @Amount, @Currency, @Method, @Provider, @ProviderReference, @ProviderPaymentReference, @Status, @CreatedAt, @UpdatedAt);
            """;
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        try
        {
            var rowVersion = await connection.ExecuteScalarAsync<byte[]>(new CommandDefinition(sql, ToParameters(payment), transaction, cancellationToken: cancellationToken));
            if (rowVersion is null) throw new DBConcurrencyException("Payment insert did not return a concurrency version.");
            payment.SetRowVersion(rowVersion);
            const string operationSql = """
                INSERT INTO IdempotencyOperations (Id, MerchantId, Operation, IdempotencyKey, FingerprintVersion, Fingerprint, PaymentId, RefundId, CreatedAt, UpdatedAt)
                OUTPUT inserted.RowVersion
                VALUES (@Id, @MerchantId, @Operation, @Key, @FingerprintVersion, @Fingerprint, @PaymentId, @RefundId, @CreatedAt, @UpdatedAt);
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
        const string sql = "SELECT Id, MerchantId, MerchantReference, Amount, Currency, Method, Provider, ProviderReference, ProviderPaymentReference, Status, CreatedAt, UpdatedAt, RowVersion FROM Payments WHERE Id = @paymentId AND MerchantId = @merchantId;";
        await using var connection = new SqlConnection(connectionString);
        var row = await connection.QuerySingleOrDefaultAsync<PaymentRow>(new CommandDefinition(sql, new { paymentId, merchantId }, cancellationToken: cancellationToken));
        return row is null ? null : ToPayment(row);
    }

    public async Task<DomainPayment?> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT Id, MerchantId, MerchantReference, Amount, Currency, Method, Provider, ProviderReference, ProviderPaymentReference, Status, CreatedAt, UpdatedAt, RowVersion FROM Payments WHERE Id = @paymentId;";
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
            UPDATE Payments SET ProviderReference = @ProviderReference, ProviderPaymentReference = @ProviderPaymentReference, Status = @Status, UpdatedAt = @UpdatedAt
            OUTPUT inserted.RowVersion
            WHERE Id = @Id AND RowVersion = @RowVersion;
            """;
        await using var connection = new SqlConnection(connectionString);
        var rowVersion = await connection.ExecuteScalarAsync<byte[]>(new CommandDefinition(sql, ToParameters(payment), cancellationToken: cancellationToken));
        if (rowVersion is null) throw new DBConcurrencyException("Payment was changed by another operation.");
        payment.SetRowVersion(rowVersion);
    }

    public async Task<RefundClaimResult> CreateRefundOrGetAsync(Refund refund, IdempotencyOperation operation, decimal reservedAmount, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString); await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        try
        {
            var reserved = await connection.ExecuteScalarAsync<decimal?>(new CommandDefinition("SELECT SUM(Amount) FROM Refunds WHERE PaymentId = @PaymentId AND Status <> 'Failed';", new { refund.PaymentId }, transaction, cancellationToken: cancellationToken)) ?? 0m;
            var paymentAmount = await connection.ExecuteScalarAsync<decimal?>(new CommandDefinition("SELECT Amount FROM Payments WHERE Id = @PaymentId AND MerchantId = @MerchantId;", new { refund.PaymentId, refund.MerchantId }, transaction, cancellationToken: cancellationToken));
            if (paymentAmount is null || reserved + reservedAmount > paymentAmount) { await transaction.RollbackAsync(cancellationToken); return new RefundClaimResult(IdempotentCreateKind.FingerprintConflict); }
            const string refundSql = "INSERT INTO Refunds (Id, PaymentId, MerchantId, Amount, Currency, Provider, ProviderReference, Status, CreatedAt, UpdatedAt) VALUES (@Id, @PaymentId, @MerchantId, @Amount, @Currency, @Provider, @ProviderReference, @Status, @CreatedAt, @UpdatedAt);";
            await connection.ExecuteAsync(new CommandDefinition(refundSql, ToParameters(refund), transaction, cancellationToken: cancellationToken));
            const string opSql = "INSERT INTO IdempotencyOperations (Id, MerchantId, Operation, IdempotencyKey, FingerprintVersion, Fingerprint, PaymentId, RefundId, CreatedAt, UpdatedAt) VALUES (@Id, @MerchantId, @Operation, @Key, @FingerprintVersion, @Fingerprint, @PaymentId, @RefundId, @CreatedAt, @UpdatedAt);";
            await connection.ExecuteAsync(new CommandDefinition(opSql, operation, transaction, cancellationToken: cancellationToken));
            await transaction.CommitAsync(cancellationToken); return new RefundClaimResult(IdempotentCreateKind.Created, refund);
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            await transaction.RollbackAsync(cancellationToken);
            var existing = await connection.QuerySingleOrDefaultAsync<Guid?>(new CommandDefinition("SELECT RefundId FROM IdempotencyOperations WHERE MerchantId=@MerchantId AND Operation=@Operation AND IdempotencyKey=@Key AND Fingerprint=@Fingerprint;", operation, cancellationToken: cancellationToken));
            return existing is null ? new RefundClaimResult(IdempotentCreateKind.FingerprintConflict) : new RefundClaimResult(IdempotentCreateKind.Replay, await GetRefundAsync(operation.MerchantId, existing.Value, cancellationToken));
        }
    }

    public async Task<Refund?> GetRefundAsync(string merchantId, Guid refundId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        var row = await connection.QuerySingleOrDefaultAsync<RefundRow>(new CommandDefinition("SELECT Id, PaymentId, MerchantId, Amount, Currency, Provider, ProviderReference, Status, CreatedAt, UpdatedAt, RowVersion FROM Refunds WHERE MerchantId=@merchantId AND Id=@refundId;", new { merchantId, refundId }, cancellationToken: cancellationToken));
        return row is null ? null : ToRefund(row);
    }

    public async Task<Refund?> GetRefundByIdAsync(Guid refundId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        var row = await connection.QuerySingleOrDefaultAsync<RefundRow>(new CommandDefinition("SELECT Id, PaymentId, MerchantId, Amount, Currency, Provider, ProviderReference, Status, CreatedAt, UpdatedAt, RowVersion FROM Refunds WHERE Id=@refundId;", new { refundId }, cancellationToken: cancellationToken));
        return row is null ? null : ToRefund(row);
    }

    public async Task<IReadOnlyList<Refund>> GetRefundsAsync(string merchantId, Guid paymentId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        var rows = await connection.QueryAsync<RefundRow>(new CommandDefinition("SELECT Id, PaymentId, MerchantId, Amount, Currency, Provider, ProviderReference, Status, CreatedAt, UpdatedAt, RowVersion FROM Refunds WHERE MerchantId=@merchantId AND PaymentId=@paymentId ORDER BY CreatedAt;", new { merchantId, paymentId }, cancellationToken: cancellationToken));
        return rows.Select(ToRefund).ToList();
    }

    public async Task UpdateRefundAsync(Refund refund, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        var version = await connection.ExecuteScalarAsync<byte[]>(new CommandDefinition("UPDATE Refunds SET ProviderReference=@ProviderReference, Status=@Status, UpdatedAt=@UpdatedAt OUTPUT inserted.RowVersion WHERE Id=@Id AND RowVersion=@RowVersion;", ToParameters(refund), cancellationToken: cancellationToken));
        if (version is null) throw new DBConcurrencyException("Refund was changed by another operation.");
    }

    public async Task<IReadOnlyList<OutcomeOutboxMessage>> GetDueOutboxMessagesAsync(DateTimeOffset now, int take, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        var rows = await connection.QueryAsync<OutcomeOutboxMessage>(new CommandDefinition("SELECT TOP (@take) * FROM OutcomeOutboxMessages WHERE DeliveredAt IS NULL AND NextAttemptAt <= @now ORDER BY OccurredAt;", new { now, take }, cancellationToken: cancellationToken));
        return rows.ToList();
    }
    public async Task UpdateOutboxMessageAsync(OutcomeOutboxMessage message, CancellationToken cancellationToken) { await using var connection = new SqlConnection(connectionString); await connection.ExecuteAsync(new CommandDefinition("UPDATE OutcomeOutboxMessages SET DeliveredAt=@DeliveredAt, NextAttemptAt=@NextAttemptAt, AttemptCount=@AttemptCount, LastError=@LastError WHERE Id=@Id;", message, cancellationToken: cancellationToken)); }

    private static object ToParameters(DomainPayment payment) => new
    {
        payment.Id, payment.MerchantId, payment.MerchantReference, Amount = payment.Amount.Value, Currency = payment.Amount.Currency,
        Method = payment.Method.ToString(), payment.Provider, payment.ProviderReference, payment.ProviderPaymentReference, Status = payment.Status.ToString(), payment.CreatedAt, payment.UpdatedAt, payment.RowVersion
    };

    private static object ToParameters(Refund refund) => new { refund.Id, refund.PaymentId, refund.MerchantId, Amount = refund.Amount.Value, Currency = refund.Amount.Currency, refund.Provider, refund.ProviderReference, Status = refund.Status.ToString(), refund.CreatedAt, refund.UpdatedAt, refund.RowVersion };

    private static DomainPayment ToPayment(PaymentRow row) => DomainPayment.Rehydrate(row.Id, row.MerchantId, row.MerchantReference, Money.Create(row.Amount, row.Currency, 4), Enum.Parse<PaymentMethod>(row.Method), row.Provider, row.ProviderReference, row.ProviderPaymentReference, Enum.Parse<PaymentStatus>(row.Status), row.CreatedAt, row.UpdatedAt, row.RowVersion);
    private static Refund ToRefund(RefundRow row)
    {
        var refund = Refund.Create(row.PaymentId, row.MerchantId, Money.Create(row.Amount, row.Currency, 4), row.Provider, row.CreatedAt);
        typeof(Refund).GetProperty(nameof(Refund.Id))!.SetValue(refund, row.Id);
        if (row.Status != nameof(RefundStatus.Pending)) refund.ApplyOutcome(Enum.Parse<RefundStatus>(row.Status), row.ProviderReference, row.UpdatedAt);
        return refund;
    }

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
        public string? ProviderPaymentReference { get; init; }
        public string Status { get; init; } = string.Empty;
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset UpdatedAt { get; init; }
        public byte[] RowVersion { get; init; } = [];
    }

    private sealed class RefundRow { public Guid Id { get; init; } public Guid PaymentId { get; init; } public string MerchantId { get; init; } = string.Empty; public decimal Amount { get; init; } public string Currency { get; init; } = string.Empty; public string Provider { get; init; } = string.Empty; public string? ProviderReference { get; init; } public string Status { get; init; } = string.Empty; public DateTimeOffset CreatedAt { get; init; } public DateTimeOffset UpdatedAt { get; init; } public byte[] RowVersion { get; init; } = []; }

    private sealed class IdempotencyRow
    {
        public int FingerprintVersion { get; init; }
        public string Fingerprint { get; init; } = string.Empty;
        public Guid PaymentId { get; init; }
    }
}
