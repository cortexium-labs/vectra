using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Vectra.Application.Abstractions.Executions;
using Vectra.Application.Abstractions.Security;
using Vectra.Domain.Agents;
using Vectra.Infrastructure.Configuration.Security;

namespace Vectra.Infrastructure.Security;

public class JwtTokenService : ITokenService
{
    private readonly AgentAuthConfiguration _agentAuthConfiguration;

    public JwtTokenService(IOptions<AgentAuthConfiguration> authSettings)
    {
        _agentAuthConfiguration = authSettings.Value;
    }

    public string GenerateToken(Agent agent)
    {
        if (_agentAuthConfiguration.Scheme == AgentAuthScheme.None)
            return string.Empty;

        var jwtSettings = _agentAuthConfiguration.Jwt;
        if (string.IsNullOrEmpty(jwtSettings.Secret))
            throw new InvalidOperationException(
                "JWT Secret is not configured. Set AgentAuth:Jwt:Secret in application settings.");

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, agent.Id.ToString()),
            new Claim("agent_name", agent.Name),
            new Claim("trust_score", agent.TrustScore.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: jwtSettings.Issuer,
            audience: jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(jwtSettings.ExpirationMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        if (_agentAuthConfiguration.Scheme == AgentAuthScheme.None)
            return null;

        var jwtSettings = _agentAuthConfiguration.Jwt;
        if (string.IsNullOrEmpty(jwtSettings.Secret))
            throw new InvalidOperationException(
                "JWT Secret is not configured. Set AgentAuth:Jwt:Secret in application settings.");

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(jwtSettings.Secret);
        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }
}