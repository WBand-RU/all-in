using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Shared;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddShared(
        this IHostApplicationBuilder builder,
        Assembly assembly
    )
    {
        builder.Services.AddScoped<ICurrentUser, CurrentUser>();

        builder.Services.AddValidatorsFromAssembly(assembly);

        return builder;
    }
}
