using System.Text.RegularExpressions;
using Api.Models;

namespace Api.Services;

/// <summary>
/// Keyword search over policies, ranked. Whole words only ("work" does not match "working"),
/// a title match counts more than a body match, and words found in most policies ("leave")
/// count for little, so "medical leave" finds POL-05 first.
/// </summary>
public static partial class PolicySearch
{
    public static List<Policy> Find(IReadOnlyList<Policy> all, string query, int take)
    {
        var words = Words(query).Distinct().ToList();
        if (words.Count == 0 || all.Count == 0) return [];
        var docs = all.Select(p => (Policy: p, Title: Words(p.Title).ToHashSet(), Body: Words(p.Body).ToHashSet())).ToList();
        double Weight(string w)
        {
            var df = docs.Count(d => d.Title.Contains(w) || d.Body.Contains(w));
            return df == 0 ? 0 : Math.Log(1.0 + (double)docs.Count / df);
        }
        var weights = words.ToDictionary(w => w, Weight);
        return docs
            .Select(d => (d.Policy, Score: words.Sum(w => weights[w] * ((d.Title.Contains(w) ? 3 : 0) + (d.Body.Contains(w) ? 1 : 0)))))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score).ThenBy(x => x.Policy.Id)
            .Take(take).Select(x => x.Policy).ToList();
    }

    static IEnumerable<string> Words(string text) =>
        WordRegex().Matches(text.ToLowerInvariant()).Select(m => m.Value);

    [GeneratedRegex(@"[a-z0-9]+")]
    private static partial Regex WordRegex();
}
