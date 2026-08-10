using DevDesk.Application.Dtos;

namespace DevDesk.Application.Interfaces;

public interface ISettingsService
{
    Task<AppPreferencesDto> GetPreferencesAsync(CancellationToken ct = default);
    Task<AppPreferencesDto> SavePreferencesAsync(AppPreferencesDto preferences, CancellationToken ct = default);
    Task<string?> GetSettingAsync(string key, CancellationToken ct = default);
    Task SetSettingAsync(string key, string value, CancellationToken ct = default);
}
