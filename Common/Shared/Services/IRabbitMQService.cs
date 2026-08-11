using RabbitMQ.Client;

namespace Shared.Services;

public interface IRabbitMQService
{
    IConnection Connection { get; }
}
