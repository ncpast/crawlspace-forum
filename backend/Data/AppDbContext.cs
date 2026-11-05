using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Board> Boards => Set<Board>();
    public DbSet<Reply> Replies => Set<Reply>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasSequence<long>("PostReplyNumbers");

        b.Entity<Board>(e =>
        {
            e.HasIndex(x => x.Slug).IsUnique();

            e.ToTable(t => t.HasCheckConstraint("CK_Boards_Slug_NotReserved", "\"Slug\" NOT IN ('api', 'assets')"));
            
            e.HasData(
                new Board { Id = 1, Slug = "tech", Name = "Technology", Description = "Hardware, software." },
                new Board { Id = 2, Slug = "random", Name = "Random", Description = "Everything else." });
        });

        b.Entity<Post>(e =>
        {
            e.Property(x => x.Id)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("nextval('\"PostReplyNumbers\"')");
            e.Property(x => x.CreatedAt).HasColumnType("timestamptz");
            e.Property(x => x.BumpedAt).HasColumnType("timestamptz");
            e.HasIndex(x => x.PublicId).IsUnique();
            e.HasIndex(x => new { x.BoardId, x.BumpedAt }).IsDescending(false, true);
            e.HasOne(x => x.Board).WithMany(x => x.Posts)
                .HasForeignKey(x => x.BoardId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Reply>(e =>
        {
            e.Property(x => x.Id)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("nextval('\"PostReplyNumbers\"')");
            e.Property(x => x.CreatedAt).HasColumnType("timestamptz");
            e.HasIndex(x => new { x.PostId, x.Id });
            e.HasOne(x => x.Post).WithMany(x => x.Replies)
                .HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}