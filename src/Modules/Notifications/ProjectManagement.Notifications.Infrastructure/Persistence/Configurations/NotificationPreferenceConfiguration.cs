using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagement.Notifications.Domain.Entities;

namespace ProjectManagement.Notifications.Infrastructure.Persistence.Configurations;

public class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> b)
    {
        b.ToTable("notification_preferences");
        b.HasKey(x => new { x.UserId, x.Type });
        b.Property(x => x.UserId).HasColumnName("user_id");
        b.Property(x => x.Type).HasColumnName("type").HasMaxLength(30).IsRequired();
        b.Property(x => x.IsEnabled).HasColumnName("is_enabled");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    }
}
