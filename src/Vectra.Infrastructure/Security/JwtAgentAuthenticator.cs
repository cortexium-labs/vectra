using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Vectra.Application.Abstractions.Executions;
using Vectra.Application.Abstractions.Security;
using Vectra.Domain.Agents;
using Vectra.BuildingBlocks.Configuration.Security;
using Vectra.BuildingBlocks.Configuration.Security.AgentAuth;

namespace Vectra.Infrastructure.Security;

public sealed class JwtAgentAuthenticator : IAgentAuthenticator
{
    private readonly AgentAuthConfiguration _options;
    private readonly ITokenService _selfSignedService;
    private readonly Lazy<ConfigurationManager<OpenIdConnectConfiguration>> _oidcConfigManager;

    public JwtAgentAuthenticator(IOptions<SecurityConfiguration> options, ITokenService selfSignedService)
    {
        _options = options.Value.AgentAuth;
        _selfSignedService = selfSignedService;

        _oidcConfigManager = new Lazy<ConfigurationManager<OpenIdConnectConfiguration>>(() =>
        {
            var metadataUrl = !string.IsNullOrWhiteSpace(_options.Jwt.MetadataUrl)
                ? _options.Jwt.MetadataUrl
                : $"{_options.Jwt.Authority.TrimEnd('/')}/.well-known/openid-configuration";

            return new ConfigurationManager<OpenIdConnectConfiguration>(
                metadataUrl,
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever { RequireHttps = !metadataUrl.StartsWith("http://localhost", StringComparison.OrdinalIgnoreCase) });
        });
    }

    public AgentAuthResult Authenticate(Agent agent)
    {
        if (_options.Provider != AgentAuthProviderType.SelfSigned)
            return AgentAuthResult.Failure(
                "Token generation is not supported for external JWT providers. " +
                "Obtain a token from the configured identity provider.");

        var token = _selfSignedService.GenerateToken(agent);
        return AgentAuthResult.Success(token);
    }

    public async Task<ClaimsPrincipal?> ValidateAsync(string credential, CancellationToken cancellationToken = default)
    {
        return _options.Provider == AgentAuthProviderType.SelfSigned
            ? _selfSignedService.ValidateToken(credential)
            : await ValidateExternalTokenAsync(credential, cancellationToken);
    }

    private async Task<ClaimsPrincipal?> ValidateExternalTokenAsync(string token, CancellationToken cancellationToken)
    {
        try
        {
            var oidcConfig = await _oidcConfigManager.Value.GetConfigurationAsync(cancellationToken);
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = oidcConfig.SigningKeys,
                ValidateIssuer = _options.Jwt.ValidateIssuer,
                ValidIssuer = _options.Jwt.ValidateIssuer ? _options.Jwt.Authority : null,
                ValidateAudience = _options.Jwt.ValidateAudience,
                ValidAudience = _options.Jwt.ValidateAudience ? _options.Jwt.Audience : null,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };

            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }
}