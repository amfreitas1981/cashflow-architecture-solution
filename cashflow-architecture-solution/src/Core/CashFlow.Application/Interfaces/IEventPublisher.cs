using System.Threading;
using System.Threading.Tasks;

namespace CashFlow.Application.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default);
}
