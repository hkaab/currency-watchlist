using CurrencyWatchlist.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CurrencyWatchlist.Infrastructure.Persistence.Configurations;

public class AlertEventConfiguration : IEntityTypeConfiguration<AlertEvent>
{
    public void Configure(EntityTypeBuilder<AlertEvent> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Rate).HasPrecision(18, 6);
        builder.Property(e => e.Message).IsRequired().HasMaxLength(500);
        builder.Property(e => e.TriggeredAt).IsRequired();

        builder.HasOne(e => e.AlertRule)
            .WithMany()
            .HasForeignKey(e => e.AlertRuleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
