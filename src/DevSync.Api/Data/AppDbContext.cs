using DevSync.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevSync.Api.Data;

/// <summary>
/// EF Core가 엔티티를 DB 테이블로 매핑할 때 사용하는 중심 클래스입니다.
/// InMemory DB와 SQL Server 모두 같은 DbContext를 사용합니다.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RoomMember> RoomMembers => Set<RoomMember>();
    public DbSet<ProjectTask> ProjectTasks => Set<ProjectTask>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(x => x.DisplayName).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(120).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.InviteToken).HasMaxLength(32).IsRequired();
            entity.HasIndex(x => x.InviteToken).IsUnique();
            entity.HasOne(x => x.OwnerUser)
                .WithMany(x => x.OwnedRooms)
                .HasForeignKey(x => x.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RoomMember>(entity =>
        {
            entity.HasKey(x => new { x.RoomId, x.UserId });
            entity.HasOne(x => x.Room)
                .WithMany(x => x.Members)
                .HasForeignKey(x => x.RoomId);
            entity.HasOne(x => x.User)
                .WithMany(x => x.RoomMembers)
                .HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<ProjectTask>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.Priority).HasMaxLength(20).IsRequired();
            entity.HasOne(x => x.Room)
                .WithMany(x => x.Tasks)
                .HasForeignKey(x => x.RoomId);
            entity.HasOne(x => x.AssigneeUser)
                .WithMany()
                .HasForeignKey(x => x.AssigneeUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.Property(x => x.Content).HasMaxLength(1000).IsRequired();
            entity.HasOne(x => x.Room)
                .WithMany(x => x.ChatMessages)
                .HasForeignKey(x => x.RoomId);
            entity.HasOne(x => x.SenderUser)
                .WithMany()
                .HasForeignKey(x => x.SenderUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(x => x.Type).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Body).HasMaxLength(300).IsRequired();
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Room)
                .WithMany()
                .HasForeignKey(x => x.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ChatMessage)
                .WithMany()
                .HasForeignKey(x => x.ChatMessageId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt });
        });
    }
}
