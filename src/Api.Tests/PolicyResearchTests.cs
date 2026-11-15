using Api.Research;
using Api.Services;

namespace Api.Tests;

public class PolicyResearchTests
{
    static PolicyResearch Make(FakeClaude fake) => new(fake, TestDb.Create(), new ClaudeOptions("claude-sonnet-5-5"));

    [Fact]
    public void Validator_flags_uncited_sentence()
    {
        var bad = PolicyResearch.UncitedSentences("You can carry forward 5 days [POL-04]. Half days are fine.");
        Assert.Single(bad);
    }

    [Fact]
    public void Validator_accepts_all_cited()
    {
        Assert.Empty(PolicyResearch.UncitedSentences("You can carry forward 5 days [POL-04]. Half days deduct 0.5 [POL-03, POL-04]."));
    }

    [Fact]
    public async Task Lead_runs_a_worker_and_returns_cited_answer()
    {
        var fake = new FakeClaude(
            FakeClaude.ToolUse("l1", "research", new { subtopic = "carry forward" }),   // lead
            FakeClaude.ToolUse("w1", "search_policies", new { query = "carry" }),     // worker
            FakeClaude.Text("Up to 5 days by 31 March [POL-04]"),                      // worker done
            FakeClaude.Text("You can carry forward up to 5 days [POL-04]."));          // lead answer
        var r = await Make(fake).AskAsync("Can I carry forward leave?");
        Assert.True(r.CitationsValid);
        Assert.Contains(r.Agents, a => a.Agent == "worker-1");
        Assert.True(r.TotalTokens > 0);
    }

    [Fact]
    public async Task Worker_limit_is_enforced()
    {
        var calls = Enumerable.Range(1, 5).Select(i => FakeClaude.ToolUse($"l{i}", "research", new { subtopic = $"t{i}" })).ToList();
        var replies = new List<string>();
        foreach (var c in calls) { replies.Add(c); replies.Add(FakeClaude.Text("finding [POL-01]")); }
        var fake = new FakeClaude([.. replies, FakeClaude.Text("Done [POL-01].")]);
        var r = await Make(fake).AskAsync("Many topics");
        Assert.True(r.Agents.Count(a => a.Agent.StartsWith("worker")) <= PolicyResearch.MaxWorkers);
    }

    [Fact]
    public void Citation_after_the_full_stop_counts()
    {
        Assert.Empty(PolicyResearch.UncitedSentences("You can carry forward 5 days. [POL-04] Half days deduct 0.5. [POL-03]"));
        Assert.Equal(["Yes, you can do both."], PolicyResearch.UncitedSentences("Yes, you can do both. Half days deduct 0.5. [POL-03]"));
    }

    [Fact]
    public void Citation_before_the_full_stop_counts() =>
        Assert.Empty(PolicyResearch.UncitedSentences("You can carry forward 5 days [POL-04]. Half days deduct 0.5 [POL-03]."));

    [Fact]
    public async Task Uncited_answer_gets_one_repair_turn()
    {
        var fake = new FakeClaude(
            FakeClaude.Text("Yes, you can do both. Half days deduct 0.5 [POL-03]."),
            FakeClaude.Text("You can do both [POL-03, POL-04]. Half days deduct 0.5 [POL-03]."));
        var r = await Make(fake).AskAsync("Can I carry forward leave and take it as half days?");
        Assert.True(r.CitationsValid);
        Assert.Equal(2, fake.Requests.Count);
        var last = fake.Requests[1];
        Assert.Contains("Yes, you can do both.", FakeClaude.MessageJson(last, last.Messages.Count - 1).ToString());
    }

    [Fact]
    public async Task Repair_happens_only_once()
    {
        var fake = new FakeClaude(FakeClaude.Text("Yes. You can."), FakeClaude.Text("Yes. You still can."), FakeClaude.Text("Never sent [POL-01]."));
        var r = await Make(fake).AskAsync("Can I?");
        Assert.False(r.CitationsValid);
        Assert.Equal(2, fake.Requests.Count);
    }

    [Fact]
    public async Task Cited_answer_needs_no_repair()
    {
        var fake = new FakeClaude(FakeClaude.Text("You get 14 days [POL-01]."));
        var r = await Make(fake).AskAsync("How many days?");
        Assert.True(r.CitationsValid);
        Assert.Single(fake.Requests);
    }
}
