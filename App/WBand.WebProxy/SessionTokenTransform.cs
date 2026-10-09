using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace WBand.WebProxy;

internal sealed class SessionTokenTransform : ITransformProvider
{
    public void ValidateRoute(TransformRouteValidationContext context) { }

    public void ValidateCluster(TransformClusterValidationContext context) { }

    public void Apply(TransformBuilderContext builder)
    {
        builder.AddRequestTransform(async context =>
        {
            context.ProxyRequest.Headers.Remove("Cookie");
            context.ProxyRequest.Headers.Remove("Authorization");
            context.ProxyRequest.Headers.Remove("X-CSRF-TOKEN");
            if (builder.Route.ClusterId == "api")
            {
                var token = await context.HttpContext.GetTokenAsync("access_token");
                context.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue(
                    "Bearer",
                    token
                );
            }
        });
    }
}
