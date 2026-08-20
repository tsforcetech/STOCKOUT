using System;
using System.Threading;
using System.Threading.Tasks;

namespace Emcore.IdentityAccess.Worker;

public interface IIntegrationEventPublisher
{
    Task PublishAsync(OutboxRow message, CancellationToken cancellationToken);
}
