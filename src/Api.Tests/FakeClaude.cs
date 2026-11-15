using System.Text.Json;
using Anthropic.Models.Messages;
using Api.Services;

namespace Api.Tests;

/// <summary>Plays back scripted Claude replies so agent code can be tested without an API key.</summary>
public class FakeClaude(params string[] replies) : IClaudeChat
{
    readonly Queue<string> _replies = new(replies);
    /// <summary>Every request the code sent, in order. Each one is the request as it was at that moment
    /// only if the code passed a copy of its history (history.ToList()), not the live list.</summary>
    public List<MessageCreateParams> Requests { get; } = [];

    public Task<Message> SendAsync(MessageCreateParams request, CancellationToken ct = default)
    {
        Requests.Add(request);
        var json = _replies.Count > 0 ? _replies.Dequeue() : Text("(no more scripted replies)");
        return Task.FromResult(JsonSerializer.Deserialize<Message>(json)!);
    }

    static string Msg(string stop, string contentJson) =>
        $$"""{"id":"msg_test","type":"message","role":"assistant","model":"claude-sonnet-5-5","stop_reason":"{{stop}}","stop_sequence":null,"usage":{"input_tokens":100,"output_tokens":20},"content":[{{contentJson}}]}""";

    static string ToolUseJson(string id, string name, object input) =>
        $$"""{"type":"tool_use","id":"{{id}}","name":"{{name}}","input":{{JsonSerializer.Serialize(input)}}}""";

    /// <summary>A text reply. Pass stop: "max_tokens" (or another stop reason) to test that path.</summary>
    public static string Text(string text, string stop = "end_turn") =>
        Msg(stop, $$"""{"type":"text","text":{{JsonSerializer.Serialize(text)}}}""");

    /// <summary>A reply with one tool call.</summary>
    public static string ToolUse(string id, string name, object input) => Msg("tool_use", ToolUseJson(id, name, input));

    /// <summary>One reply with several tool calls, e.g. ToolUses(("t1", "get_leave_balance", new { }), ("t2", "check_policy", new { topic = "carry forward" })).</summary>
    public static string ToolUses(params (string Id, string Name, object Input)[] calls) =>
        Msg("tool_use", string.Join(",", calls.Select(c => ToolUseJson(c.Id, c.Name, c.Input))));

    /// <summary>One message of a request as API JSON (field names such as "tool_use_id").
    /// Serialize messages one by one: serializing the whole MessageCreateParams does not give API field names.</summary>
    public static JsonElement MessageJson(MessageCreateParams request, int index) =>
        JsonDocument.Parse(JsonSerializer.Serialize(request.Messages[index])).RootElement.Clone();

    /// <summary>The tool_result blocks in the last message of a request: (tool_use_id, content as text, is_error).</summary>
    public static List<(string ToolUseId, string Content, bool IsError)> ToolResults(MessageCreateParams request)
    {
        var msg = MessageJson(request, request.Messages.Count - 1);
        var list = new List<(string, string, bool)>();
        if (!msg.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array) return list;
        foreach (var b in content.EnumerateArray())
        {
            if (!b.TryGetProperty("type", out var t) || t.GetString() != "tool_result") continue;
            var id = b.TryGetProperty("tool_use_id", out var i) ? i.GetString() ?? "" : "";
            var text = b.TryGetProperty("content", out var c) ? (c.ValueKind == JsonValueKind.String ? c.GetString() ?? "" : c.GetRawText()) : "";
            var err = b.TryGetProperty("is_error", out var e) && e.ValueKind == JsonValueKind.True;
            list.Add((id, text, err));
        }
        return list;
    }
}
