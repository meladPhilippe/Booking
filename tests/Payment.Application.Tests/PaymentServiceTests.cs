using Payment.Application.Abstractions;
using Payment.Application.Payments;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using DomainPayment = Payment.Domain.Entities.Payment;

namespace Payment.Application.Tests;

public sealed class PaymentServiceTests
{
    [Fact]
    public async Task Creates_a_provider_neutral_payment()
    {
        var repository = new InMemoryRepository();
        var service = new PaymentService(repository, new Resolver(new FakeProvider(PaymentStatus.Succeeded)), new Merchant(), new Currencies(), new ReturnUrls(), TimeProvider.System);
        var result = await service.CreateAsync(new CreatePaymentCommand("reservation-7", 500m, "EGP", PaymentMethod.Card, "https://merchant.test/return", "https://merchant.test/cancel", "key-1"), CancellationToken.None);
        Assert.Equal(PaymentStatus.Succeeded, result.Payment.Status);
        Assert.Equal("reservation-7", result.Payment.MerchantReference);
        Assert.Equal("fake", result.Payment.Provider);
        Assert.NotEqual(Guid.Empty, result.Payment.PaymentId);
    }

    [Fact]
    public async Task Rejects_an_unsupported_currency()
    {
        var service = new PaymentService(new InMemoryRepository(), new Resolver(new FakeProvider(PaymentStatus.Succeeded)), new Merchant(), new Currencies(), new ReturnUrls(), TimeProvider.System);
        var exception = await Assert.ThrowsAsync<PaymentValidationException>(() => service.CreateAsync(new CreatePaymentCommand("reference", 1m, "XXX", PaymentMethod.Card, "https://merchant.test/return", "https://merchant.test/cancel", "key-1"), CancellationToken.None));
        Assert.Equal("unsupported_currency", exception.Code);
    }

    [Fact]
    public async Task Replaying_the_same_key_returns_the_existing_payment_without_a_second_provider_call()
    {
        var repository = new InMemoryRepository();
        var provider = new CountingProvider();
        var service = new PaymentService(repository, new Resolver(provider), new Merchant(), new Currencies(), new ReturnUrls(), TimeProvider.System);

        var first = await service.CreateAsync(new CreatePaymentCommand("reservation-7", 500m, "EGP", PaymentMethod.Card, "https://merchant.test/return", "https://merchant.test/cancel", "key-1"), CancellationToken.None);
        var replay = await service.CreateAsync(new CreatePaymentCommand("reservation-7", 500m, "EGP", PaymentMethod.Card, "https://merchant.test/return", "https://merchant.test/cancel", "key-1"), CancellationToken.None);

        Assert.False(first.IsReplay);
        Assert.True(replay.IsReplay);
        Assert.Equal(first.Payment.PaymentId, replay.Payment.PaymentId);
        Assert.Equal(1, provider.Calls);
    }

    [Fact]
    public async Task Reusing_a_key_for_a_different_request_returns_a_controlled_conflict()
    {
        var service = new PaymentService(new InMemoryRepository(), new Resolver(new FakeProvider(PaymentStatus.Succeeded)), new Merchant(), new Currencies(), new ReturnUrls(), TimeProvider.System);
        await service.CreateAsync(new CreatePaymentCommand("reservation-7", 500m, "EGP", PaymentMethod.Card, "https://merchant.test/return", "https://merchant.test/cancel", "key-1"), CancellationToken.None);

        var exception = await Assert.ThrowsAsync<PaymentConflictException>(() =>
            service.CreateAsync(new CreatePaymentCommand("reservation-7", 600m, "EGP", PaymentMethod.Card, "https://merchant.test/return", "https://merchant.test/cancel", "key-1"), CancellationToken.None));

        Assert.Equal("idempotency_key_reused", exception.Code);
    }

    private sealed class Merchant : IMerchantContext { public string MerchantId => "merchant-test"; }
    private sealed class Currencies : ICurrencyRules { public bool TryGetDecimalPlaces(string currency, out int decimalPlaces) { decimalPlaces = 2; return currency == "EGP"; } }
    private sealed class ReturnUrls : IReturnUrlPolicy { public bool IsAllowed(Uri returnUrl) => returnUrl.Host == "merchant.test"; }
    private sealed class Resolver(IPaymentProvider provider) : IPaymentProviderResolver
    {
        public IPaymentProvider ResolveForNewPayment(PaymentMethod method) => provider;
        public IPaymentProvider ResolveByKey(string providerKey) => provider;
    }
    private sealed class FakeProvider(PaymentStatus status) : IPaymentProvider
    {
        public string ProviderKey => "fake";
        public Task<ProviderPaymentOutcome> CreateAsync(ProviderPaymentRequest request, CancellationToken cancellationToken) => Task.FromResult(new ProviderPaymentOutcome(status, "fake-ref"));
    }
    private sealed class CountingProvider : IPaymentProvider
    {
        public int Calls { get; private set; }
        public string ProviderKey => "fake";
        public Task<ProviderPaymentOutcome> CreateAsync(ProviderPaymentRequest request, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new ProviderPaymentOutcome(PaymentStatus.Succeeded, "fake-ref"));
        }
    }
    private sealed class InMemoryRepository : IPaymentRepository
    {
        private readonly Dictionary<Guid, DomainPayment> _payments = [];
        private readonly Dictionary<string, (string Fingerprint, Guid PaymentId)> _operations = [];
        public Task<IdempotentCreateResult> CreateOrGetAsync(DomainPayment payment, IdempotencyOperation operation, CancellationToken cancellationToken)
        {
            var key = $"{operation.MerchantId}:{operation.Operation}:{operation.Key}";
            if (_operations.TryGetValue(key, out var existing))
                return Task.FromResult(existing.Fingerprint == operation.Fingerprint
                    ? new IdempotentCreateResult(IdempotentCreateKind.Replay, _payments[existing.PaymentId])
                    : new IdempotentCreateResult(IdempotentCreateKind.FingerprintConflict));
            _payments.Add(payment.Id, payment);
            _operations.Add(key, (operation.Fingerprint, payment.Id));
            return Task.FromResult(new IdempotentCreateResult(IdempotentCreateKind.Created, payment));
        }
        public Task<IdempotentCreateResult> ClaimOperationAsync(IdempotencyOperation operation, CancellationToken cancellationToken) => Task.FromResult(new IdempotentCreateResult(IdempotentCreateKind.Created));
        public Task<DomainPayment?> GetAsync(string merchantId, Guid paymentId, CancellationToken cancellationToken) => Task.FromResult(_payments.GetValueOrDefault(paymentId) is { } payment && payment.MerchantId == merchantId ? payment : null);
        public Task<DomainPayment?> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken) => Task.FromResult(_payments.GetValueOrDefault(paymentId));
        public Task<bool> TryRecordWebhookEventAsync(ProviderWebhookEvent webhookEvent, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task UpdateAsync(DomainPayment payment, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
