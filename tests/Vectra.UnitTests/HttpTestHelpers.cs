using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Vectra.UnitTests;

internal static class HttpTestHelpers
{
    /// <summary>
    /// Creates a DefaultHttpContext with a minimal service provider that satisfies
    /// IResult.ExecuteAsync (which requires JsonOptions etc. from DI).
    /// </summary>
    public static DefaultHttpContext CreateContext()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();

        // Register the JSON options that IResult implementations rely on
        services.ConfigureHttpJsonOptions(_ => { });
        services.AddSingleton<Microsoft.AspNetCore.Http.Json.JsonOptions>();

        var provider = services.BuildServiceProvider();

        return new DefaultHttpContext
        {
            RequestServices = provider,
            Response = { Body = new MemoryStream() }
        };
    }

    public static async Task<int> ExecuteAndGetStatusCodeAsync(IResult result)
    {
        var context = CreateContext();
        await result.ExecuteAsync(context);
        return context.Response.StatusCode;
    }

    public static int ExecuteAndGetStatusCode(IResult result)
        => ExecuteAndGetStatusCodeAsync(result).GetAwaiter().GetResult();
}
