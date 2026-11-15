using System.Text.Json;
using Api.Helpdesk;
using Api.Services;
using Xunit;

namespace Api.Tests;

public class HelpdeskToolsTests
{
    [Fact]
    public async Task Any_tool_failure_comes_back_as_an_error_result_not_an_exception()
    {
        var db = TestDb.Create();
        var tools = new HelpdeskTools(db, new LeaveService(db));
        var input = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>("""{"start_date": 5, "end_date": 6}""")!;
        var outcome = await tools.RunAsync("apply_leave", input, 6);
        Assert.True(outcome.IsError);
    }
}
