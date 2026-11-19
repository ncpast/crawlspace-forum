using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Backend.Tests;

public class TestClientIpStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(
        Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.Use(async (context, nextMiddleware) =>
            {
                if (context.Request.Headers.TryGetValue(
                    "X-Test-Client-IP", out var value))
                {
                    if (IPAddress.TryParse(value.ToString(), out var ip))
                    {
                        context.Connection.RemoteIpAddress = ip;
                    }
                }

                await nextMiddleware();
            });

            next(app);
        };
    }
}