namespace Payment.Domain.Enums;

public enum RefundStatus
{
    Pending,
    Succeeded,
    Failed,
    RequiresReconciliation
}
