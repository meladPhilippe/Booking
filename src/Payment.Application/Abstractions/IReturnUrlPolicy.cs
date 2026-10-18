namespace Payment.Application.Abstractions;

public interface IReturnUrlPolicy
{
    bool IsAllowed(Uri returnUrl);
}
