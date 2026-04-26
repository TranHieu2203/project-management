namespace ProjectManagement.Capacity.Domain.Entities;

public class CapacityOverride
{
    public Guid Id { get; private set; }
    public Guid ResourceId { get; private set; }
    public DateOnly DateFrom { get; private set; }
    public DateOnly DateTo { get; private set; }
    public string TrafficLight { get; private set; } = default!;
    public string OverriddenBy { get; private set; } = default!;
    public DateTime OverriddenAt { get; private set; }

    private CapacityOverride() { }

    public static CapacityOverride Create(Guid resourceId, DateOnly dateFrom, DateOnly dateTo,
        string trafficLight, string overriddenBy) => new()
    {
        Id = Guid.NewGuid(),
        ResourceId = resourceId,
        DateFrom = dateFrom,
        DateTo = dateTo,
        TrafficLight = trafficLight,
        OverriddenBy = overriddenBy,
        OverriddenAt = DateTime.UtcNow,
    };
}
