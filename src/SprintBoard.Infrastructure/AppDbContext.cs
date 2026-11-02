// src/SprintBoard.Infrastructure/AppDbContext.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SprintBoard.Domain.Models;

namespace SprintBoard.Infrastructure;

public sealed class AppUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = "";
}

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Board> Boards => Set<Board>();
    public DbSet<BoardMember> BoardMembers => Set<BoardMember>();
    public DbSet<WorkItem> WorkItems => Set<WorkItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Board>().HasKey(b => b.Id);
        builder.Entity<BoardMember>().HasKey(m => new { m.BoardId, m.UserId });
        builder.Entity<WorkItem>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.Title).HasMaxLength(120);
            e.HasIndex(i => i.BoardId);
        });
    }
}