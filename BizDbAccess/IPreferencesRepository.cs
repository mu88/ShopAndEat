using DataLayer.EfClasses;

namespace BizDbAccess;

public interface IPreferencesRepository
{
    Task<IReadOnlyList<ShoppingPreference>> GetAllPreferencesAsync(string scope, string storeKey, CancellationToken cancellationToken = default);

    Task UpsertPreferenceAsync(ShoppingPreference preference, CancellationToken cancellationToken = default);

    Task<bool> DeletePreferenceAsync(string scope, string key, string storeKey, CancellationToken cancellationToken = default);
}
