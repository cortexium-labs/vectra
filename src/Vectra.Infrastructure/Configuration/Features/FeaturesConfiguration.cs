using Vectra.Infrastructure.Configuration.Features.Hitl;

namespace Vectra.Infrastructure.Configuration.Features;

public class FeaturesConfiguration
{
    public HitlConfiguration Hitl { get; set; } = new();
}