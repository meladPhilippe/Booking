namespace Payment.Application.Abstractions;

public enum IdempotentCreateKind
{
    Created,
    Replay,
    FingerprintConflict,
}
