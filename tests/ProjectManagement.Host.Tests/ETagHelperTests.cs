using ProjectManagement.Shared.Infrastructure.OptimisticLocking;

namespace ProjectManagement.Host.Tests;

public sealed class ETagHelperTests
{
    [Fact]
    public void Generate_ReturnsQuotedVersion()
    {
        Assert.Equal("\"1\"", ETagHelper.Generate(1));
        Assert.Equal("\"42\"", ETagHelper.Generate(42));
    }

    [Fact]
    public void ParseIfMatch_WithQuotedVersion_ReturnsParsedLong()
    {
        Assert.Equal(1L, ETagHelper.ParseIfMatch("\"1\""));
        Assert.Equal(42L, ETagHelper.ParseIfMatch("\"42\""));
    }

    [Fact]
    public void ParseIfMatch_WithNull_ReturnsNull()
    {
        Assert.Null(ETagHelper.ParseIfMatch(null));
    }

    [Fact]
    public void ParseIfMatch_WithEmptyString_ReturnsNull()
    {
        Assert.Null(ETagHelper.ParseIfMatch(""));
    }

    [Fact]
    public void ParseIfMatch_WithNonNumeric_ReturnsNull()
    {
        Assert.Null(ETagHelper.ParseIfMatch("\"abc\""));
    }

    [Fact]
    public void GenerateThenParse_RoundTrip()
    {
        var version = 99L;
        var etag = ETagHelper.Generate(version);
        Assert.Equal(version, ETagHelper.ParseIfMatch(etag));
    }
}
