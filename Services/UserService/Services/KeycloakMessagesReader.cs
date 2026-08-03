using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Wolverine;

namespace UserService.Services;

public class KeycloakMessagesReader : BackgroundService
{
    public record CommonMessage(string Type);

    public record LoginMessage(long Time, Guid UserId, LoginMessageDetails Details);

    public record LoginMessageDetails(string Username);

    private readonly IMessageBus messageBus;
    private readonly Dictionary<string, Func<string, Task>> handlers;
    private readonly IServiceProvider serviceProvider;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        const string REALM_NAME = "wband"; // TODO: move to the envs
        const string EXCHANGE_NAME = "amq.topic";

        using var scope = serviceProvider.CreateScope();
        var connection = scope.ServiceProvider.GetRequiredService<IConnection>();

        using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(
            EXCHANGE_NAME,
            ExchangeType.Topic,
            true,
            cancellationToken: stoppingToken
        );

        var queueName = (
            await channel.QueueDeclareAsync(cancellationToken: stoppingToken)
        ).QueueName;

        await channel.QueueBindAsync(
            queueName,
            EXCHANGE_NAME,
            $"KK.EVENT.CLIENT.{REALM_NAME}.SUCCESS.#",
            cancellationToken: stoppingToken
        );

        Console.WriteLine("Ожидание сообщений...");

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += Handle;

        // 5. Включаем потребление
        await channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: true,
            consumer: consumer,
            cancellationToken: stoppingToken
        );

        Console.ReadLine();
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

        var messageType = common.Type;
        var handler = handlers[messageType];

        await handler(message);
    }

    private async Task HandleLogin(string message)
    {
        var login = JsonSerializer.Deserialize<LoginMessage>(message);
        await messageBus.PublishAsync(login);
    }

    const string LoginJson =
        @"
    {
      ""@class"" : ""com.github.aznamier.keycloak.event.provider.EventClientNotificationMqMsg"",
      ""time"" : 1779791436757,
      ""type"" : ""LOGIN"",
      ""realmId"" : ""4fb149ea-6897-41ca-9682-eb552d9f239c"",
      ""clientId"" : ""web"",
      ""userId"" : ""4f32656c-8061-473d-a840-2f810bf1ea93"",
      ""sessionId"" : ""OTIT1ATY-FZsy45_ObEzEfOF"",
      ""ipAddress"" : ""172.18.0.1"",
      ""details"" : {
        ""auth_method"" : ""openid-connect"",
        ""auth_type"" : ""code"",
        ""response_type"" : ""code"",
        ""redirect_uri"" : ""http://localhost/app"",
        ""consent"" : ""no_consent_required"",
        ""code_id"" : ""OTIT1ATY-FZsy45_ObEzEfOF"",
        ""username"" : ""test@wband.ru"",
        ""response_mode"" : ""fragment""
      }
    }";

    public KeycloakMessagesReader(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        var scope = serviceProvider.CreateScope();
        messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        handlers = new() { ["LOGIN"] = HandleLogin };
    }
}
