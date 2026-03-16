using MediatR;

namespace Payments.StripeIntegration.Shared
{
    public interface IDomainEvent : INotification
    {
    }
}
