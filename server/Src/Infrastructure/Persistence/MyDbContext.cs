using Domain.Entities;
using Domain.Interfaces.Utility;
using Infrastructure.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Persistence;

public class MyDbContextFactory : IDesignTimeDbContextFactory<MyDbContext>
{
    public MyDbContext CreateDbContext(string[] args)
    {
        // For design-time (migrations), read connection string from environment variable
        // Falls back to localhost if not set
        var envHelper = new EnvHelper();

        var builder = new DbContextOptionsBuilder<MyDbContext>();
        builder.UseNpgsql(EnvHelper.LoadAndGetConnectionString(true));
        
        return new MyDbContext(builder.Options);
    }
}
public class MyDbContext(DbContextOptions<MyDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<ChatRoom> ChatRooms => Set<ChatRoom>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<ChatRoomMember> ChatRoomMembers => Set<ChatRoomMember>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        
        modelBuilder.Entity<User>();
        
        modelBuilder.Entity<ChatRoom>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // many-to-one - many ChatRooms can be created by one User
            entity.HasOne(e => e.CreatedBy)
                .WithMany()
                .HasForeignKey(e => e.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

                // Make ChatRoom name unique
                entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // one-to-many - one ChatRoom can have many ChatMessages, but each ChatMessage belongs to one ChatRoom
            entity.HasOne(e => e.Room)
                .WithMany(r => r.Messages)
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // many-to-one - many ChatMessages can be sent by one User, but each ChatMessage has only one Sender 
            entity.HasOne(e => e.Sender)
                .WithMany()
                .HasForeignKey(e => e.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.RoomId, e.CreatedAt });
        });

        modelBuilder.Entity<ChatRoomMember>(entity =>
        {
            entity.HasKey(e => new { e.RoomId, e.UserId });
            
            // many-to-one - many ChatRoomMembers can belong to one ChatRoom, but each ChatRoomMember belongs to one ChatRoom
            entity.HasOne(e => e.Room)
                .WithMany(r => r.Members)
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // many-to-one - many ChatRoomMembers can belong to one User, but each ChatRoomMember belongs to one User
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.Recipient)
                .WithMany()
                .HasForeignKey(e => e.RecipientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.RecipientId, e.IsRead, e.CreatedAt });
        });
    }
}