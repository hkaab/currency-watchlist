using CurrencyWatchlist.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CurrencyWatchlist.Infrastructure.Persistence.Configurations;

public class WatchlistItemConfiguration : IEntityTypeConfiguration<WatchlistItem>
{
    public void Configure(EntityTypeBuilder<WatchlistItem> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.BaseCurrency).IsRequired().HasMaxLength(3);
        builder.Property(i => i.QuoteCurrency).IsRequired().HasMaxLength(3);
        builder.Property(i => i.CreatedAt).IsRequired();

        builder.HasIndex(i => new { i.WatchlistId, i.BaseCurrency, i.QuoteCurrency }).IsUnique();
        builder.HasIndex(i => new { i.BaseCurrency, i.QuoteCurrency });

        builder.HasMany(i => i.AlertRules)
            .WithOne(a => a.WatchlistItem)
            .HasForeignKey(a => a.WatchlistItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
