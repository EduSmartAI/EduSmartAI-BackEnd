namespace BaseService.Common.Settings;

public static class EnvLoader
{
    public static void Load(string envFileName = ".env")
    {
        var dir = Directory.GetCurrentDirectory();
        string? envFilePath = null;

        while (dir != null)
        {
            var candidate = Path.Combine(dir, envFileName);
            if (File.Exists(candidate))
            {
                envFilePath = candidate;
                break;
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        if (envFilePath == null || !File.Exists(envFilePath))
            return;

        var lines = File.ReadAllLines(envFilePath);
        foreach (var line in lines)
        {
            // Ignore empty lines or comments
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                continue;

            var parts = line.Split('=', 2);
            if (parts.Length != 2)
                continue;

            var key = parts[0].Trim();
            var value = parts[1].Trim();

            // Set environment variables (runtime only)
            Environment.SetEnvironmentVariable(key, value);
        }
    }
}