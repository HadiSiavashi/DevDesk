namespace DevDesk.Application.Options;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string Provider { get; set; } = "SqlServer";
    public string ConnectionString { get; set; } = string.Empty;
    public bool ApplyMigrationsOnStartup { get; set; } = true;
    public bool SeedSampleData { get; set; }
}
