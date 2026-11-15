namespace Api;

/// <summary>Loads KEY=VALUE lines from the nearest .env file (repo root) into environment variables.</summary>
public static class DotEnv
{
    /// <summary>Set to "1" to skip .env. The test host sets it, so tests never use your DATABASE_URL or keys.</summary>
    public const string SkipVariable = "ACME_SKIP_DOTENV";

    public static void Load()
    {
        if (Environment.GetEnvironmentVariable(SkipVariable) == "1") return;
        var file = Find(Directory.GetCurrentDirectory());
        if (file is null) return;
        foreach (var line in File.ReadAllLines(file))
        {
            var t = line.Trim();
            if (t.Length == 0 || t.StartsWith('#') || !t.Contains('=')) continue;
            var i = t.IndexOf('=');
            var key = t[..i].Trim();
            var value = t[(i + 1)..].Trim().Trim('"');
            if (Environment.GetEnvironmentVariable(key) is null) Environment.SetEnvironmentVariable(key, value);
        }
    }

    /// <summary>The nearest .env at or above the folder, or null.</summary>
    public static string? Find(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, ".env"))) dir = dir.Parent;
        return dir is null ? null : Path.Combine(dir.FullName, ".env");
    }
}
