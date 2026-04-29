namespace ProjectManagement.Reporting.Domain.Entities;

public class AlertPreference
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string AlertType { get; private set; } = string.Empty;
    public bool Enabled { get; private set; }
    public int? ThresholdDays { get; private set; }

    public static AlertPreference Create(
        Guid userId,
        string alertType,
        bool enabled = true,
        int? thresholdDays = null)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AlertType = alertType,
            Enabled = enabled,
            ThresholdDays = thresholdDays,
        };

    public void Update(bool enabled, int? thresholdDays)
    {
        Enabled = enabled;
        ThresholdDays = thresholdDays;
    }
}
