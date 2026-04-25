namespace ProjectManagement.Shared.Infrastructure.OptimisticLocking;

public static class ETagHelper
{
    public static string Generate(long version) => $"\"{version}\"";

    public static long? ParseIfMatch(string? ifMatchHeader)
    {
        if (string.IsNullOrWhiteSpace(ifMatchHeader))
            return null;

        var trimmed = ifMatchHeader.Trim().Trim('"');
        return long.TryParse(trimmed, out var version) ? version : null;
    }
}
