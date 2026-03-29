using DataLayer.EF;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;

namespace BizDbAccess.Concrete;

public class PreferencesRepository(EfCoreContext context) : IPreferencesRepository
{
    public async Task<IReadOnlyList<ShoppingPreference>> GetAllPreferencesAsync(string scope, string storeKey, CancellationToken cancellationToken = default)
    {
        var query = context.ShoppingPreferences.AsQueryable();

        if (!string.IsNullOrEmpty(scope))
        {
            query = query.Where(preference => preference.Scope == scope);
        }

        if (!string.IsNullOrEmpty(storeKey))
        {
            // Include shop-specific preferences AND shop-overarching ones (StoreKey == null)
            query = query.Where(preference => preference.StoreKey == storeKey || preference.StoreKey == null);
        }

        return await query
            .OrderByDescending(preference => preference.UsageCount)
            .ToListAsync(cancellationToken);
    }

    public async Task UpsertPreferenceAsync(ShoppingPreference preference, CancellationToken cancellationToken = default)
    {
        var existing = await context.ShoppingPreferences
            .FirstOrDefaultAsync(existing => existing.Scope == preference.Scope && existing.Key == preference.Key && existing.StoreKey == preference.StoreKey, cancellationToken);

        if (existing != null)
        {
            existing.Value = preference.Value;
            existing.UsageCount++;
        }
        else
        {
            context.ShoppingPreferences.Add(preference);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeletePreferenceAsync(string scope, string key, string storeKey, CancellationToken cancellationToken = default)
    {
        var preference = await context.ShoppingPreferences
            .FirstOrDefaultAsync(preference => preference.Scope == scope && preference.Key == key && preference.StoreKey == storeKey, cancellationToken);

        if (preference == null)
        {
            return false;
        }

        context.ShoppingPreferences.Remove(preference);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
