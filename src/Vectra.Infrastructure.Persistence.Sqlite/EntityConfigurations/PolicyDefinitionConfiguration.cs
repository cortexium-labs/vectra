using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vectra.Domain.Policies;

namespace Vectra.Infrastructure.Persistence.Sqlite.EntityConfigurations;

public class PolicyDefinitionConfiguration : IEntityTypeConfiguration<PolicyDefinition>
{
    public void Configure(EntityTypeBuilder<PolicyDefinition> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).IsRequired();
        builder.Property(e => e.Name).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Description).HasMaxLength(1024);

        builder.HasMany(p => p.Rules).WithOne(r => r.Policy).HasForeignKey(r => r.PolicyId);
    }
}