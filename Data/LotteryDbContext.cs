using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using LotteryTracker.API.Models;
using System;
using System.Linq;

namespace LotteryTracker.API.Data;

public class LotteryDbContext : DbContext
{
    public LotteryDbContext(DbContextOptions<LotteryDbContext> options) : base(options)
    {
    }

    public DbSet<LotteryDrawing> Drawings { get; set; }
    public DbSet<StatisticalAnalysis> Analyses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var intArrayComparer = new ValueComparer<int[]>(
            (left, right) => left.SequenceEqual(right),
            value => value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
            value => value.ToArray());

        var doubleArrayComparer = new ValueComparer<double[]>(
            (left, right) => left.SequenceEqual(right),
            value => value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
            value => value.ToArray());

        modelBuilder.Entity<LotteryDrawing>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DrawDate).IsRequired();
            entity.Property(e => e.WinningNumbers)
                .HasConversion(
                    value => string.Join(',', value),
                    value => value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(int.Parse)
                        .ToArray())
                .Metadata.SetValueComparer(intArrayComparer);
            entity.Property(e => e.WinningNumbers).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
        });

        modelBuilder.Entity<StatisticalAnalysis>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AnalyzedDate).IsRequired();
            entity.Property(e => e.NumberFrequencies)
                .HasConversion(
                    value => string.Join(',', value),
                    value => value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(double.Parse)
                        .ToArray())
                .Metadata.SetValueComparer(doubleArrayComparer);
            entity.Property(e => e.NumberFrequencies).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
        });
    }
}
