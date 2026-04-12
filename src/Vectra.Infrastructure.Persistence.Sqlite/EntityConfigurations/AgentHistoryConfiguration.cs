using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vectra.Domain.Agents;

namespace Vectra.Infrastructure.Persistence.Sqlite.EntityConfigurations;

public class AgentHistoryConfiguration : IEntityTypeConfiguration<AgentHistory>
{
    public void Configure(EntityTypeBuilder<AgentHistory> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(gb => gb.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.AgentId).IsRequired();
        builder.Property(h => h.WindowStart).IsRequired();
        builder.Property(h => h.WindowDurationSeconds).HasDefaultValue(60);
        builder.Property(h => h.TotalRequests).HasDefaultValue(0);
        builder.Property(h => h.ViolationCount).HasDefaultValue(0);
        builder.Property(h => h.AverageRiskScore).HasDefaultValue(0.0);

        builder.HasIndex(h => new { h.AgentId, h.WindowStart }).IsUnique();

        builder.HasOne(h => h.Agent)
               .WithMany(a => a.AgentHistories)
               .HasForeignKey(h => h.AgentId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}