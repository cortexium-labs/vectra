using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vectra.Domain.AuditTrails;

namespace Vectra.Infrastructure.Persistence.Sqlite.EntityConfigurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditTrail>
{
    public void Configure(EntityTypeBuilder<AuditTrail> builder)
    {
        builder.HasKey(e => e.Id);
    }
}