using CurrencyWatchlist.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CurrencyWatchlist.Infrastructure.Persistence.Configurations;

public class AlertRuleConfiguration : IEntityTypeConfiguration<AlertRule>
{
    public void Configure(EntityTypeBuilder<AlertRule> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Condition).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(a => a.Threshold).HasPrecision(18, 6);
        builder.Property(a => a.IsActive).IsRequired();
        builder.Property(a => a.CreatedAt).IsRequired();

        builder.HasIndex(a => new { a.WatchlistItemId, a.IsActive });
    }
}
