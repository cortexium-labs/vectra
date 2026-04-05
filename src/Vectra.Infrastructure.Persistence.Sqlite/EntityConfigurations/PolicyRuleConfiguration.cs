using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vectra.Domain.Policies;

namespace Vectra.Infrastructure.Persistence.Sqlite.EntityConfigurations;

public class PolicyRuleConfiguration : IEntityTypeConfiguration<PolicyRule>
{
    public void Configure(EntityTypeBuilder<PolicyRule> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).IsRequired();
        builder.Property(e => e.PolicyId).IsRequired();
        builder.Property(e => e.Name).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Effect).IsRequired();
        builder.Property(e => e.Reason).IsRequired().HasMaxLength(1024);

        builder.HasMany(r => r.Conditions).WithOne(c => c.Rule).HasForeignKey(c => c.RuleId);
    }
}