using Api.Helpdesk;
using Api.Services;
using Xunit;

namespace Api.Tests;

public class FakeClaudeTests
{
    [Fact]
    public async Task ToolResults_reads_the_blocks_the_agent_sent_back()
    {
        var claude = new FakeClaude(
            FakeClaude.ToolUses(("t1", "check_policy", new { topic = "medical leave" }), ("t2", "check_policy", new { topic = "remote work" })),
            FakeClaude.Text("Done."));
        var db = TestDb.Create();
        await new HelpdeskAgent(claude, new HelpdeskTools(db, new LeaveService(db)), new ClaudeOptions("claude-sonnet-5-5"))
            .RunAsync(6, [new ChatTurn("user", "Two questions")]);
        var results = FakeClaude.ToolResults(claude.Requests[1]);
        Assert.Equal(["t1", "t2"], results.Select(r => r.ToolUseId));
        Assert.Contains("POL-05", results[0].Content);
    }

    [Fact]
    public void Text_can_carry_another_stop_reason() =>
        Assert.Contains("\"max_tokens\"", FakeClaude.Text("cut off", stop: "max_tokens"));
}
