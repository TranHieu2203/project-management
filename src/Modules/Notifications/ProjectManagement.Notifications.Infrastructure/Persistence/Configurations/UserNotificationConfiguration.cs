using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagement.Notifications.Domain.Entities;

namespace ProjectManagement.Notifications.Infrastructure.Persistence.Configurations;

public class UserNotificationConfiguration : IEntityTypeConfiguration<UserNotification>
{
    public void Configure(EntityTypeBuilder<UserNotification> b)
    {
        b.ToTable("user_notifications", "notifications");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.RecipientUserId).HasColumnName("recipient_user_id").IsRequired();
        b.Property(x => x.Type).HasColumnName("type").HasMaxLength(30).IsRequired();
        b.Property(x => x.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        b.Property(x => x.Body).HasColumnName("body").IsRequired();
        b.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(30);
        b.Property(x => x.EntityId).HasColumnName("entity_id");
        b.Property(x => x.ProjectId).HasColumnName("project_id");
        b.Property(x => x.IsRead).HasColumnName("is_read").HasDefaultValue(false);
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.ReadAt).HasColumnName("read_at");

        b.HasIndex(x => new { x.RecipientUserId, x.IsRead, x.CreatedAt })
            .HasDatabaseName("ix_user_notifications_user_read_created");
    }
}
