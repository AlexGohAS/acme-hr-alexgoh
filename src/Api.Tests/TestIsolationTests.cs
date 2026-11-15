using System.Net.Http.Json;
using Xunit;

namespace Api.Tests;

/// <summary>Tests must never share a database or reach the Render database in .env.</summary>
public class TestIsolationTests
{
    [Fact]
    public async Task Two_factories_get_two_databases()
    {
        using var a = new ApiFactory();
        using var b = new ApiFactory();
        (await a.CreateClient().GetAsync("/health")).EnsureSuccessStatusCode();
        (await b.CreateClient().GetAsync("/health")).EnsureSuccessStatusCode();
        Assert.NotEqual(a.DbFile, b.DbFile);
        Assert.True(a.UsesOwnFile && b.UsesOwnFile);
    }

    [Fact]
    public async Task Test_host_uses_Sqlite_never_the_database_in_env()
    {
        using var api = new ApiFactory();
        var health = await api.CreateClient().GetFromJsonAsync<Health>("/health");
        Assert.Contains("Sqlite", health!.Provider);
    }

    [Fact]
    public void Dotenv_is_skipped_while_tests_run()
    {
        _ = new ApiFactory();
        Assert.Equal("1", Environment.GetEnvironmentVariable(Api.DotEnv.SkipVariable));
    }

    record Health(string Status, string Database, string Provider);
}
