using System.Diagnostics.CodeAnalysis;
using Vectra.Infrastructure;
using Vectra.Infrastructure.SecretManagement;

namespace Vectra.Extensions;

[ExcludeFromCodeCoverage]
public static class SecretManagementExtensions
{
    public static WebApplicationBuilder AddVectraSecretManagement(this WebApplicationBuilder builder)
    {
        builder.Services.AddSecretManagement();
        var secretManager = builder.Services.BuildServiceProvider().GetRequiredService<ISecretManagementService>();
        secretManager.Current?.Configure(builder.Configuration);

        return builder;
    }
}

