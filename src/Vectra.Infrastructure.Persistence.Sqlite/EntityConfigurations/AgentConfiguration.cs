using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Vectra.Domain.Agents;

namespace Vectra.Infrastructure.Persistence.Sqlite.EntityConfigurations;

public class AgentConfiguration : IEntityTypeConfiguration<Agent>
{
    public void Configure(EntityTypeBuilder<Agent> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(gb => gb.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.Name).IsRequired().HasMaxLength(128);
        builder.Property(e => e.OwnerId).IsRequired();

        var statusConverter = new ValueConverter<AgentStatus, string>(
            v => v.ToString(),
            v => (AgentStatus)Enum.Parse(typeof(AgentStatus), v, true)
        );

        builder.Property(gb => gb.Status)
            .HasColumnType("TEXT")
            .HasMaxLength(50)
            .HasConversion(statusConverter)
            .HasDefaultValue(AgentStatus.Active);
    }
}