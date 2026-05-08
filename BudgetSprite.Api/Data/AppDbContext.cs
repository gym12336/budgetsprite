using BudgetSprite.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BudgetSprite.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<FinAccount> FinAccounts => Set<FinAccount>();
    public DbSet<Record> Records => Set<Record>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<RecordImage> RecordImages => Set<RecordImage>();
    public DbSet<RecurringRule> RecurringRules => Set<RecurringRule>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<User>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Username).HasMaxLength(50);
            e.Property(u => u.PasswordHash).HasMaxLength(256);
            e.Property(u => u.Email).HasMaxLength(100);
            e.Property(u => u.Nickname).HasMaxLength(50);
            e.Property(u => u.AvatarUrl).HasMaxLength(300);
        });

        mb.Entity<Category>(e =>
        {
            e.Property(c => c.Name).HasMaxLength(50);
            e.Property(c => c.Icon).HasMaxLength(50);
            e.Property(c => c.Color).HasMaxLength(10);
            e.HasOne(c => c.Parent)
             .WithMany(c => c.Children)
             .HasForeignKey(c => c.ParentId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(c => c.User)
             .WithMany(u => u.Categories)
             .HasForeignKey(c => c.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<FinAccount>(e =>
        {
            e.Property(a => a.Name).HasMaxLength(50);
            e.Property(a => a.Balance).HasPrecision(18, 2);
            e.Property(a => a.Note).HasMaxLength(200);
        });

        mb.Entity<Record>(e =>
        {
            e.Property(r => r.Amount).HasPrecision(18, 2);
            e.Property(r => r.Note).HasMaxLength(500);
            e.Property(r => r.Tags).HasMaxLength(200);
            e.HasQueryFilter(r => !r.IsDeleted);
        });

        mb.Entity<Budget>(e =>
        {
            e.Property(b => b.Amount).HasPrecision(18, 2);
            e.Property(b => b.YearMonth).HasMaxLength(7);
            e.HasOne(b => b.Category)
             .WithMany()
             .HasForeignKey(b => b.CategoryId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        mb.Entity<RecordImage>(e =>
        {
            e.Property(i => i.ImageUrl).HasMaxLength(300);
        });

        mb.Entity<RecurringRule>(e =>
        {
            e.Property(r => r.Amount).HasPrecision(18, 2);
            e.Property(r => r.Note).HasMaxLength(200);
        });
    }
}
