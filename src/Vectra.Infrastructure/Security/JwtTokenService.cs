using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Vectra.Application.Abstractions.Executions;
using Vectra.Domain.Agents;
using Vectra.BuildingBlocks.Configuration.Security;
using Vectra.BuildingBlocks.Configuration.Security.AgentAuth;

namespace Vectra.Infrastructure.Security;

public class JwtTokenService : ITokenService
{
    private readonly AgentAuthConfiguration _agentAuthConfiguration;

    public JwtTokenService(IOptions<SecurityConfiguration> authSettings)
    {
        _agentAuthConfiguration = authSettings.Value.AgentAuth;
    }

    public string GenerateToken(Agent agent)
    {
        if (string.IsNullOrEmpty(_agentAuthConfiguration.SelfSigned.Secret))
            throw new InvalidOperationException(
                "JWT Secret is not configured. Set Security:AgentAuth:SelfSigned:Secret in application settings.");

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, agent.Id.ToString()),
            new Claim("agent_name", agent.Name),
            new Claim("trust_score", agent.TrustScore.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_agentAuthConfiguration.SelfSigned.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _agentAuthConfiguration.SelfSigned.Issuer,
            audience: _agentAuthConfiguration.SelfSigned.Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(_agentAuthConfiguration.SelfSigned.Expiration),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        if (string.IsNullOrEmpty(_agentAuthConfiguration.SelfSigned.Secret))
            throw new InvalidOperationException(
                "JWT Secret is not configured. Set Security:AgentAuth:SelfSigned:Secret in application settings.");

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_agentAuthConfiguration.SelfSigned.Secret);
        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _agentAuthConfiguration.SelfSigned.Issuer,
                ValidateAudience = true,
                ValidAudience = _agentAuthConfiguration.SelfSigned.Audience,
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