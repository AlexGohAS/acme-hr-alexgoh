using Api.Data;
using Api.Services;
using Xunit;

namespace Api.Tests;

public class PolicySearchTests
{
    [Theory]
    [InlineData("medical leave", "POL-05")]
    [InlineData("What do I need to submit when I take medical leave?", "POL-05")]
    [InlineData("work from home", "POL-11")]
    [InlineData("remote work", "POL-11")]
    [InlineData("carry forward", "POL-04")]
    [InlineData("half day", "POL-03")]
    public void Top_hit_is_the_matching_policy(string query, string expected) =>
        Assert.Equal(expected, PolicySearch.Find(PolicyDocs.All, query, 3)[0].Id);

    [Fact]
    public void Whole_words_only() =>
        Assert.DoesNotContain(PolicySearch.Find(PolicyDocs.All, "work", 15), p => p.Id is "POL-01" or "POL-02");

    [Fact]
    public void No_match_returns_empty() =>
        Assert.Empty(PolicySearch.Find(PolicyDocs.All, "xylophone", 3));
}
