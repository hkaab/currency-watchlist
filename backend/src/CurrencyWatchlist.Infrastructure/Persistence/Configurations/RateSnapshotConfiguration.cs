using CurrencyWatchlist.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CurrencyWatchlist.Infrastructure.Persistence.Configurations;

public class RateSnapshotConfiguration : IEntityTypeConfiguration<RateSnapshot>
{
    public void Configure(EntityTypeBuilder<RateSnapshot> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.BaseCurrency).IsRequired().HasMaxLength(3);
        builder.Property(s => s.QuoteCurrency).IsRequired().HasMaxLength(3);
        builder.Property(s => s.Rate).HasPrecision(18, 6);
        builder.Property(s => s.SourceTimestamp).IsRequired();
        builder.Property(s => s.FetchedAt).IsRequired();

        builder.HasIndex(s => new { s.BaseCurrency, s.QuoteCurrency, s.SourceTimestamp });
    }
}
