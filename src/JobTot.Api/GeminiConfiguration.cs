namespace JobTot.Api;

public static class GeminiConfiguration
{
    public static void AddGeminiSecrets(this ConfigurationManager configuration)
    {
        // Default hosting only loads User Secrets in Development. Local launches without
        // a launch profile still need the Gemini credentials saved for this application.
        // Fill missing Gemini settings only; preserve explicit environment/CLI settings.
        var secrets = new ConfigurationBuilder().AddUserSecrets("jobtot-api").Build();
        using var lifetime = (IDisposable)secrets;
        var missing = secrets.GetSection("Gemini").AsEnumerable()
            .Where(entry => entry.Value is not null && configuration[entry.Key] is null);
        configuration.AddInMemoryCollection(missing.ToArray());
    }
}
