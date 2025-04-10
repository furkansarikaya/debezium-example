using Microsoft.EntityFrameworkCore;
using Transfer.Shared.Models;

namespace Transfer.API.Write.Infrastructure;

public class TransferDbContext : DbContext
{
    public TransferDbContext(DbContextOptions<TransferDbContext> options) : base(options)
    {
    }

    public DbSet<TransferEntity> Transfers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TransferEntity>(entity =>
        {
            entity.ToTable("transfers");
            
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.FromAccount)
                .IsRequired();
                
            entity.Property(e => e.ToAccount)
                .IsRequired();
                
            entity.Property(e => e.Amount)
                .HasPrecision(12, 2)
                .IsRequired();
                
            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");
                
            entity.Property(e => e.Description)
                .HasMaxLength(500);
        });
    }
} 