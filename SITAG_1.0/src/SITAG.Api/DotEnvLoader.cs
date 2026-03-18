namespace SITAG.Api;

/// <summary>
/// Minimal .env file loader for local Development.
/// Reads KEY=VALUE pairs and sets them as process environment variables
/// only when they are not already set — this preserves explicit OS overrides.
///
/// This class is intentionally simple: no interpolation, no quoting rules
/// beyond trimming, and no support for multiline values.
/// It is never called in Production (guarded in Program.cs).
/// </summary>
internal static class DotEnvLoader
{
    internal static void Load(string filePath)
    {
        if (!File.Exists(filePath))
            return;

        foreach (var rawLine in File.ReadAllLines(filePath))
        {
            var line = rawLine.Trim();

            // Skip empty lines and comments
            if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                continue;

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex < 1)
                continue;

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();

            // Don't overwrite variables already set by the OS or IDE
            if (Environment.GetEnvironmentVariable(key) is null)
                Environment.SetEnvironmentVariable(key, value);
        }
    }
}
