using System.Text.Json;
using DevDesk.Application.Abstractions;
using DevDesk.Application.Dtos;
using DevDesk.Application.Interfaces;
using DevDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevDesk.Application.Services;

public sealed class SettingsService(IDevDeskDbContext db) : ISettingsService
{
    public const string PreferencesKey = "app.preferences";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public async Task<AppPreferencesDto> GetPreferencesAsync(CancellationToken ct = default)
    {
        var raw = await GetSettingAsync(PreferencesKey, ct);
        if (string.IsNullOrWhiteSpace(raw))
            return new AppPreferencesDto();

        try
        {
            return JsonSerializer.Deserialize<AppPreferencesDto>(raw, JsonOptions) ?? new AppPreferencesDto();
        }
        catch (JsonException)
        {
            return new AppPreferencesDto();
        }
    }

    public async Task<AppPreferencesDto> SavePreferencesAsync(AppPreferencesDto preferences, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        var json = JsonSerializer.Serialize(preferences, JsonOptions);
        await SetSettingAsync(PreferencesKey, json, ct);
        return preferences;
    }

    public async Task<string?> GetSettingAsync(string key, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Setting key is required.", nameof(key));

        var setting = await db.AppSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Key == key, ct);
        return setting?.Value;
    }

    public async Task SetSettingAsync(string key, string value, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Setting key is required.", nameof(key));

        var setting = await db.AppSettings.FirstOrDefaultAsync(s => s.Key == key, ct);
        if (setting is null)
        {
            db.AppSettings.Add(new AppSetting { Key = key, Value = value });
        }
        else
        {
            setting.Value = value;
        }

        await db.SaveChangesAsync(ct);
    }
}
