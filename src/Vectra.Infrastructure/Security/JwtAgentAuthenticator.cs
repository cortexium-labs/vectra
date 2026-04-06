using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Vectra.Application.Abstractions.Security;
using Vectra.Domain.Agents;
using Vectra.Infrastructure.Configuration.Security;

namespace Vectra.Infrastructure.Security;

public sealed class JwtAgentAuthenticator : IAgentAuthenticator
{
    private readonly JwtConfiguration _config;
    private readonly JwtTokenService _selfSignedService;
    private readonly Lazy<ConfigurationManager<OpenIdConnectConfiguration>> _oidcConfigManager;

    public JwtAgentAuthenticator(IOptions<JwtConfiguration> config, JwtTokenService selfSignedService)
    {
        _config = config.Value;
        _selfSignedService = selfSignedService;

        _oidcConfigManager = new Lazy<ConfigurationManager<OpenIdConnectConfiguration>>(() =>
        {
            var metadataUrl = !string.IsNullOrWhiteSpace(_config.MetadataUrl)
                ? _config.MetadataUrl
                : $"{_config.Authority.TrimEnd('/')}/.well-known/openid-configuration";

            return new ConfigurationManager<OpenIdConnectConfiguration>(
                metadataUrl,
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever { RequireHttps = !metadataUrl.StartsWith("http://localhost", StringComparison.OrdinalIgnoreCase) });
        });
    }

    public AgentAuthScheme Scheme => AgentAuthScheme.Jwt;

    public AgentAuthResult Authenticate(Agent agent)
    {
        if (_config.Provider != JwtProviderType.SelfSigned)
            return AgentAuthResult.Failure(
                "Token generation is not supported for external JWT providers. " +
                "Obtain a token from the configured identity provider.");

        var token = _selfSignedService.GenerateToken(agent);
        return AgentAuthResult.Success(token);
    }

    public async Task<ClaimsPrincipal?> ValidateAsync(string credential, CancellationToken cancellationToken = default)
    {
        return _config.Provider == JwtProviderType.SelfSigned
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
                ValidateIssuer = _config.ValidateIssuer,
                ValidIssuer = _config.ValidateIssuer ? _config.Authority : null,
                ValidateAudience = _config.ValidateAudience,
                ValidAudience = _config.ValidateAudience ? _config.Audience : null,
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