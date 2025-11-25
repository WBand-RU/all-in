using Microsoft.Extensions.Hosting;
using Wolverine;
using Wolverine.RabbitMQ;

namespace MessagesCommon;

public static class ServiceCollectionExtensions
{
    public static void AddMessaging(
        this IHostApplicationBuilder builder,
        string url,
        int port,
        string username,
        string password
    )
    {
        builder.UseWolverine(x =>
        {
            x.UseRabbitMq(x =>
            {
                x.HostName = url;
                x.Port = port;
                x.UserName = username;
                x.Password = password;
            });
        });
    }
}
