namespace ProjectManagement.Capacity.Domain.Entities;

public class ForecastArtifact
{
    public Guid Id { get; private set; }
    public int Version { get; private set; }
    public DateTime ComputedAt { get; private set; }
    public string Status { get; private set; } = default!;  // "Pending" | "Succeeded" | "Failed"
    public string? Payload { get; private set; }            // JSON
    public string? ErrorMessage { get; private set; }

    private ForecastArtifact() { }

    public static ForecastArtifact Create(int version) => new()
    {
        Id = Guid.NewGuid(),
        Version = version,
        ComputedAt = DateTime.UtcNow,
        Status = "Pending",
    };

    public void MarkSucceeded(string payload)
    {
        Status = "Succeeded";
        Payload = payload;
    }

    public void MarkFailed(string error)
    {
        Status = "Failed";
        ErrorMessage = error;
    }
}
