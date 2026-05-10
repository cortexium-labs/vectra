using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vectra.Application.Abstractions.Dispatchers;
using Vectra.Application.Features.Agents.AgentsList;
using Vectra.Application.Features.Agents.AssignPolicy;
using Vectra.Application.Features.Agents.DeleteAgent;
using Vectra.Application.Features.Agents.RegisterAgent;
using Vectra.Application.Features.Authentications.GenerateToken;
using Vectra.Application.Features.Policies.PoliciesList;
using Vectra.Application.Features.Policies.PolicyDetails;
using Vectra.BuildingBlocks.Results;
using VoidType = Vectra.Application.Abstractions.Dispatchers.Void;

namespace Vectra.Application.UnitTests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddVectraApplication_ShouldRegisterAllHandlers()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddVectraApplication();

        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IActionHandler<AgentsListRequest, PaginatedResult<AgentsListResult>>));
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IActionHandler<CreateAgentRequest, Result<CreateAgentResult>>));
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IActionHandler<AssignPolicyRequest, Result<VoidType>>));
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IActionHandler<DeleteAgentRequest, Result<VoidType>>));
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IActionHandler<GenerateTokenRequest, Result<GenerateTokenResult>>));
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IActionHandler<PoliciesListRequest, PaginatedResult<PoliciesListResult>>));
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IActionHandler<PolicyDetailsRequest, Result<PolicyDetailsResult>>));
    }

    [Fact]
    public void AddVectraApplication_ShouldReturnSameServiceCollection()
    {
        var services = new ServiceCollection();
        var returned = services.AddVectraApplication();

        returned.Should().BeSameAs(services);
    }
}
