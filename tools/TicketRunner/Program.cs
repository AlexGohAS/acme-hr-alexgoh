// Runs the Lab 2 test tickets against the running API and prints pass / fail.
// Usage: dotnet run --project tools/TicketRunner [-- --api http://localhost:5080] [--verbose]
// Repeatable: the dates in the tickets are filled in with a free future week for the employees who
// apply for leave, and the Pending leave this run creates is rejected again at the end (as HR).
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

var api = args.SkipWhile(a => a != "--api").Skip(1).FirstOrDefault() ?? "http://localhost:5080";
var verbose = args.Contains("--verbose");
var path = Path.Combine(AppContext.BaseDirectory, "tickets.json");
if (!File.Exists(path)) path = Path.Combine("tools", "TicketRunner", "tickets.json");
var json = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var tickets = JsonSerializer.Deserialize<List<Ticket>>(File.ReadAllText(path), json)!;
using var http = new HttpClient { BaseAddress = new Uri(api), Timeout = TimeSpan.FromMinutes(2) };

// Each ticket is asked by a different employee, so sign in as that employee (training password).
var password = Environment.GetEnvironmentVariable("SEED_USER_PASSWORD") ?? "Acme@2026";
var emails = new Dictionary<int, string>();
var tokens = new Dictionary<int, string>();
async Task<string> Token(string email)
{
    var r = await http.PostAsJsonAsync("/auth/login", new { email, password });
    if (!r.IsSuccessStatusCode) throw new Exception($"sign-in failed for {email}: {(int)r.StatusCode}");
    return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString()!;
}
var hrToken = await Token("dina.ismail4@acme-hr.example");
async Task<JsonElement> HrGet(string url)
{
    using var req = new HttpRequestMessage(HttpMethod.Get, url);
    req.Headers.Authorization = new("Bearer", hrToken);
    return await (await http.SendAsync(req)).Content.ReadFromJsonAsync<JsonElement>();
}
foreach (var e in (await HrGet("/employees")).EnumerateArray()) emails[e.GetProperty("id").GetInt32()] = e.GetProperty("email").GetString()!;

// Pick the first week, at least two weeks ahead, where nobody who applies in a ticket has leave yet.
var appliers = tickets.Where(t => t.Text.Contains('{')).Select(t => t.EmployeeId).Distinct().ToList();
var taken = new List<(DateOnly Start, DateOnly End)>();
foreach (var id in appliers)
    foreach (var r in (await HrGet($"/leave?employeeId={id}")).EnumerateArray())
        if (r.GetProperty("status").GetString() != "Rejected")
            taken.Add((DateOnly.Parse(r.GetProperty("startDate").GetString()!), DateOnly.Parse(r.GetProperty("endDate").GetString()!)));
var today = DateOnly.FromDateTime(DateTime.Today);
var mon = today.AddDays(14 + ((int)DayOfWeek.Monday - (int)today.AddDays(14).DayOfWeek + 7) % 7);
while (taken.Any(t => t.Start <= mon.AddDays(13) && t.End >= mon)) mon = mon.AddDays(14);
var dates = new Dictionary<string, DateOnly>
{
    ["{mon}"] = mon, ["{tue}"] = mon.AddDays(1), ["{wed}"] = mon.AddDays(2), ["{thu}"] = mon.AddDays(3), ["{fri}"] = mon.AddDays(4),
    ["{sat}"] = mon.AddDays(5), ["{mon2}"] = mon.AddDays(7),
};
string Fill(string text) => dates.Aggregate(text, (s, d) => s.Replace(d.Key, d.Value.ToString("yyyy-MM-dd")));
Console.WriteLine($"Ticket dates use the week of {mon:yyyy-MM-dd}.\n");

var pass = 0;
Console.WriteLine($"{"#",-3} {"Result",-6} {"Turns",-5} {"Tools",-40} Note");
foreach (var t in tickets)
{
    ChatReply? r = null;
    try
    {
        if (!tokens.TryGetValue(t.EmployeeId, out var token)) tokens[t.EmployeeId] = token = await Token(emails[t.EmployeeId]);
        using var chat = new HttpRequestMessage(HttpMethod.Post, "/helpdesk/chat") { Content = JsonContent.Create(new { messages = new[] { new { role = "user", text = Fill(t.Text) } } }) };
        chat.Headers.Authorization = new("Bearer", token);
        var res = await http.SendAsync(chat);
        if (!res.IsSuccessStatusCode) throw new Exception($"HTTP {(int)res.StatusCode}: {Trim(await res.Content.ReadAsStringAsync(), 200)}");
        r = (await res.Content.ReadFromJsonAsync<ChatReply>(json))!;
        var tools = string.Join(",", r.ToolCalls.Select(c => c.Name));
        var outputs = string.Join(" ", r.ToolCalls.Select(c => c.Output));
        var notes = new List<string>();
        if (t.ExpectTool is { } et && !r.ToolCalls.Any(c => c.Name == et)) notes.Add($"expected tool {et}");
        if (t.ExpectOutput is { } eo && !outputs.Contains(eo)) notes.Add($"expected output {eo}");
        if (t.ExpectReply is { } er && !r.Reply.Contains(er)) notes.Add($"expected reply to cite {er}");
        if (t.ExpectReplyAny is { } any && !any.Any(a => r.Reply.Contains(a, StringComparison.OrdinalIgnoreCase))) notes.Add("expected a weekend / working day explanation");
        if (t.ForbidOutput is { } fo && Regex.IsMatch(outputs, fo)) notes.Add("injected request was approved!");
        if (r.Turns > 8) notes.Add("went past MaxTurns");
        var ok = notes.Count == 0; if (ok) pass++;
        Console.WriteLine($"{t.Id,-3} {(ok ? "PASS" : "FAIL"),-6} {r.Turns,-5} {Trim(tools, 40),-40} {string.Join("; ", notes)}");
        if (!ok || verbose) Detail(r);
    }
    catch (Exception e)
    {
        Console.WriteLine($"{t.Id,-3} {"ERROR",-6} {"",-5} {"",-40} {e.Message}");
        if (r is not null) Detail(r);
    }
}
Console.WriteLine($"\n{pass}/{tickets.Count} tickets as expected.");

// Clean up: reject the Pending leave this run created, so the next run starts the same way.
var cleaned = 0;
foreach (var id in appliers)
    foreach (var lr in (await HrGet($"/leave?employeeId={id}")).EnumerateArray())
    {
        var start = DateOnly.Parse(lr.GetProperty("startDate").GetString()!);
        if (lr.GetProperty("status").GetString() != "Pending" || start < mon || start > mon.AddDays(13)) continue;
        using var rej = new HttpRequestMessage(HttpMethod.Post, $"/leave/{lr.GetProperty("id").GetInt32()}/reject");
        rej.Headers.Authorization = new("Bearer", hrToken);
        if ((await http.SendAsync(rej)).IsSuccessStatusCode) cleaned++;
    }
Console.WriteLine($"Cleanup: rejected {cleaned} Pending request(s) from this run. Approved ones (tickets 2 and 4, 2.5 days) stay; a fresh local database resets them.");

static void Detail(ChatReply r)
{
    Console.WriteLine($"      reply: {Trim(r.Reply.Replace('\n', ' '), 300)}");
    foreach (var c in r.ToolCalls) Console.WriteLine($"      {c.Name}{(c.IsError ? " (error)" : "")}: {Trim(c.Output, 200)}");
}
static string Trim(string s, int n) => s.Length <= n ? s : s[..(n - 1)] + "…";

record Ticket(int Id, int EmployeeId, string Text, string? ExpectTool, string? ExpectOutput, string? ExpectReply, string[]? ExpectReplyAny, string? ForbidOutput);
record ToolCall(string Name, string Input, string Output, bool IsError);
record ChatReply(string Reply, List<ToolCall> ToolCalls, int Turns, string StopReason);
