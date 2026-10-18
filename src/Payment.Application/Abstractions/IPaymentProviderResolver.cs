using Payment.Domain.Enums;

namespace Payment.Application.Abstractions;

public interface IPaymentProviderResolver
{
    IPaymentProvider Resolve(PaymentMethod method);
}
