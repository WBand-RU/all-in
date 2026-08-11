using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using WBand.Modules.UserModule.Configurations;
using WBand.Modules.UserModule.Services.KeycloakHandlers;
using Wolverine;

namespace WBand.Modules.UserModule.Services;

public class KeycloakMessagesReader : BackgroundService
{
    public record CommonMessage([property: JsonPropertyName("type")] string Type);

    public record LoginMessage(
        [property: JsonPropertyName("time")] long Time,
        [property: JsonPropertyName("userId")] Guid UserId,
        [property: JsonPropertyName("details")] LoginMessageDetails Details
    );

    public record LoginMessageDetails([property: JsonPropertyName("username")] string Username);

    private readonly IMessageBus messageBus;
    private readonly Dictionary<string, IKeycloakHandler> handlers;
    private readonly IServiceProvider serviceProvider;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var options = scope.ServiceProvider.GetRequiredService<
                    IOptions<KeycloakMessagesReaderConfiguration>
                >();
                var rabbit = scope.ServiceProvider.GetRequiredService<IConnectionFactory>();
                await using var connection = await rabbit.CreateConnectionAsync(stoppingToken);
                var logger = scope.ServiceProvider.GetRequiredService<
                    ILogger<KeycloakMessagesReader>
                >();

                using var channel = await connection.CreateChannelAsync(
                    cancellationToken: stoppingToken
                );

                var exchange = options.Value.Exchange;
                await channel.ExchangeDeclareAsync(
                    exchange,
                    ExchangeType.Topic,
                    true,
                    cancellationToken: stoppingToken
                );

                var queueName = (
                    await channel.QueueDeclareAsync(cancellationToken: stoppingToken)
                ).QueueName;

                var realm = options.Value.Realm;
                await channel.QueueBindAsync(
                    queueName,
                    exchange,
                    $"KK.EVENT.CLIENT.{realm}.SUCCESS.#",
                    cancellationToken: stoppingToken
                );

                Log.Information("Создана очередь {QueueName}", queueName);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += Handle;

                // 5. Включаем потребление
                await channel.BasicConsumeAsync(
                    queue: queueName,
                    autoAck: true,
                    consumer: consumer,
                    cancellationToken: stoppingToken
                );

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                Log.Information("KeycloakMessagesReader stopped");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception by reading messages from Keycloak");

                // Если упала сеть или RabbitMQ временно недоступен,
                // делаем небольшую паузу перед следующей итерацией цикла while,
                // чтобы не спамить логами и не грузить процессор
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    Log.Information("KeycloakMessagesReader stopped");
                }
            }
        }
    }

    private async Task Handle(object sender, BasicDeliverEventArgs ea)
    {
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        Console.WriteLine($"[x] User is logged in: {message}");

        var common = JsonSerializer.Deserialize<CommonMessage>(message);
        if (common is null)
        {
            Console.WriteLine($"[ERROR] Cannot deserialize: {message}");
            return;
        }

        if (string.IsNullOrEmpty(common.Type))
        {
            Log.Warning("Keycloak message type is empty");
            return;
        }

        var messageType = common.Type;
        if (!handlers.TryGetValue(messageType, out var handler))
        {
            return;
        }

        await handler.Handle(message);
    }

    public KeycloakMessagesReader(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        var scope = serviceProvider.CreateScope();
        messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        handlers = new List<IKeycloakHandler>
        {
            scope.ServiceProvider.GetRequiredService<KeycloakRegisterHandler>(),
            scope.ServiceProvider.GetRequiredService<KeycloakLoginHandler>(),
            scope.ServiceProvider.GetRequiredService<KeycloakLogoutHandler>(),
        }.ToDictionary(x => x.Key, x => x);
    }
}
