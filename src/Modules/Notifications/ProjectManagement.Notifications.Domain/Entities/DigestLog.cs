namespace ProjectManagement.Notifications.Domain.Entities;

public class DigestLog
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string DigestType { get; private set; } = string.Empty;
    public int IsoWeek { get; private set; }
    public int Year { get; private set; }
    public DateTime SentAt { get; private set; }

    public static DigestLog Create(Guid userId, string digestType, int isoWeek, int year)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DigestType = digestType,
            IsoWeek = isoWeek,
            Year = year,
            SentAt = DateTime.UtcNow
        };
}
