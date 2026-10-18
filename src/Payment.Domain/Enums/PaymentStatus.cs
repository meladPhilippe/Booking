namespace Payment.Domain.Enums;

public enum PaymentStatus
{
    Pending,
    Processing,
    RequiresAction,
    Succeeded,
    Failed,
    Cancelled,
    RequiresReconciliation
}
