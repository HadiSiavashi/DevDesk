namespace DevDesk.Domain.Entities;

/// <summary>
/// Key/value application preferences persisted in the database.
/// </summary>
public class AppSetting
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
