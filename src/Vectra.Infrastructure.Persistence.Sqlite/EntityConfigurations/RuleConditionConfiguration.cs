using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vectra.Domain.Policies;

namespace Vectra.Infrastructure.Persistence.Sqlite.EntityConfigurations;

public class RuleConditionConfiguration : IEntityTypeConfiguration<RuleCondition>
{
    public void Configure(EntityTypeBuilder<RuleCondition> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).IsRequired();
        builder.Property(e => e.RuleId).IsRequired();
        builder.Property(e => e.LogicalOperator).HasMaxLength(4);
        builder.Property(e => e.Attribute).HasMaxLength(4096);
        builder.Property(e => e.Operator).HasMaxLength(16);

        builder.Property(e => e.ValueJson).HasColumnType("jsonb");
    }
}