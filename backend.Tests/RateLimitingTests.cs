using System.Net;
using System.Net.Http.Json;
using Xunit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests;

public class RateLimitingTests
{
    [Fact]
    public async Task CreatePost_ExceedingLimit_Returns429()
    {
        using var factory = new ForumTestFactory(useRealRateLimits: true);
        using var client = factory.CreateClient();

        for (int i = 0; i < 3; i++)
        {
            var response = await client.PostAsJsonAsync(
                "/boards/nonexistent/posts",
                new
                {
                    title = "Test",
                    content = "Test content",
                    author = "Tester"
                }
            );

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        var rejectedResponse = await client.PostAsJsonAsync(
            "/boards/nonexistent/posts",
            new
            {
                title = "Test",
                content = "Test content",
                author = "Tester"
            }
        );

        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            rejectedResponse.StatusCode
        );
    }

    [Fact]
    public async Task DifferentIPs_HaveIndependentRateLimits()
    {
        using var baseFactory = new ForumTestFactory(useRealRateLimits: true);

        using var factory = baseFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddTransient<IStartupFilter, TestClientIpStartupFilter>();
            });
        });

        using var client = factory.CreateClient();

        // Client A uses all three permits
        for (int i = 0; i < 3; i++)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/boards/nonexistent/posts"
            );

            request.Headers.Add("X-Test-Client-IP", "192.0.2.10");
            request.Content = JsonContent.Create(new
            {
                title = "Test",
                content = "Test content",
                author = "Tester"
            });

            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // Client A exceeds its limit
        using (var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/boards/nonexistent/posts"))
        {
            request.Headers.Add("X-Test-Client-IP", "192.0.2.10");
            request.Content = JsonContent.Create(new
            {
                title = "Test",
                content = "Test content",
                author = "Tester"
            });

            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        }

        // Client B should still have its own permits
        using (var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/boards/nonexistent/posts"))
        {
            request.Headers.Add("X-Test-Client-IP", "192.0.2.20");
            request.Content = JsonContent.Create(new
            {
                title = "Test",
                content = "Test content",
                author = "Tester"
            });

            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    [Fact]
    public async Task UntrustedForwardedHeader_DoesNotBypassRateLimit()
    {
        using var baseFactory = new ForumTestFactory(useRealRateLimits: true);

        using var factory = baseFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddTransient<IStartupFilter, TestClientIpStartupFilter>();
            });
        });

        using var client = factory.CreateClient();

        for (int i = 0; i < 4; i++)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/boards/nonexistent/posts"
            );

            // The actual simulated client IP stays the same.
            request.Headers.Add("X-Test-Client-IP", "192.0.2.10");

            // The client attempts to change its identity.
            request.Headers.Add(
                "X-Forwarded-For",
                $"203.0.113.{10 + i}"
            );

            request.Content = JsonContent.Create(new
            {
                title = "Test",
                content = "Test content",
                author = "Tester"
            });

            using var response = await client.SendAsync(request);

            if (i < 3)
            {
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            }
            else
            {
                Assert.Equal(
                    HttpStatusCode.TooManyRequests,
                    response.StatusCode
                );
            }
        }
    }
}