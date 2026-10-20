using Payment.Domain.Enums;

namespace Payment.Application.Abstractions;

public interface IPaymentProviderResolver
{
    IPaymentProvider ResolveForNewPayment(PaymentMethod method);
    IPaymentProvider ResolveByKey(string providerKey);
}
