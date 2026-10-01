using DataLayer.EF;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;

namespace ServiceLayer.Concrete;

public class SimpleCrudHelper(EfCoreContext dbContext)
{
    public async Task<TDtoOut> CreateAsync<TDtoIn, TIn, TDtoOut>(
        TDtoIn newDto,
        Func<TDtoIn, TIn> toEntity,
        Func<TIn, TDtoOut> toDto,
        CancellationToken cancellationToken = default)
        where TIn : class
    {
        var addedEntity = dbContext.Add(toEntity(newDto));
        await dbContext.SaveChangesAsync(cancellationToken);

        return toDto(addedEntity.Entity);
    }

    public Task DeleteAsync<TIn>(int idToDelete, CancellationToken cancellationToken = default)
        where TIn : class, IHasId<int>
        => DeleteInternalAsync<TIn>(idToDelete, cancellationToken);

    public Task DeleteAsync<TIn>(UnitId idToDelete, CancellationToken cancellationToken = default)
        where TIn : class, IHasId<UnitId>
        => DeleteInternalAsync<TIn>(idToDelete, cancellationToken);

    public Task DeleteAsync<TIn>(ArticleGroupId idToDelete, CancellationToken cancellationToken = default)
        where TIn : class, IHasId<ArticleGroupId>
        => DeleteInternalAsync<TIn>(idToDelete, cancellationToken);

    public Task DeleteAsync<TIn>(RecipeId idToDelete, CancellationToken cancellationToken = default)
        where TIn : class, IHasId<RecipeId>
        => DeleteInternalAsync<TIn>(idToDelete, cancellationToken);

    public Task DeleteAsync<TIn>(MealId idToDelete, CancellationToken cancellationToken = default)
        where TIn : class, IHasId<MealId>
        => DeleteInternalAsync<TIn>(idToDelete, cancellationToken);

    public async Task<IReadOnlyList<TDtoOut>> GetAllAsDtoAsync<TIn, TDtoOut>(
        Func<TIn, TDtoOut> toDto,
        CancellationToken cancellationToken = default)
        where TIn : class
    {
        var entities = await dbContext.Set<TIn>().ToListAsync(cancellationToken);

        return entities.Select(toDto).ToList();
    }

    public Task<TOut> FindAsync<TOut>(int id, CancellationToken cancellationToken = default)
        where TOut : class, IHasId<int>
        => FindInternalAsync<TOut>(id, cancellationToken);

    public Task<TOut> FindAsync<TOut>(UnitId id, CancellationToken cancellationToken = default)
        where TOut : class, IHasId<UnitId>
        => FindInternalAsync<TOut>(id, cancellationToken);

    public Task<TOut> FindAsync<TOut>(ArticleGroupId id, CancellationToken cancellationToken = default)
        where TOut : class, IHasId<ArticleGroupId>
        => FindInternalAsync<TOut>(id, cancellationToken);

    public Task<TOut> FindAsync<TOut>(ArticleId id, CancellationToken cancellationToken = default)
        where TOut : class, IHasId<ArticleId>
        => FindInternalAsync<TOut>(id, cancellationToken);

    public Task<TOut> FindAsync<TOut>(StoreId id, CancellationToken cancellationToken = default)
        where TOut : class, IHasId<StoreId>
        => FindInternalAsync<TOut>(id, cancellationToken);

    public Task<TOut> FindAsync<TOut>(RecipeId id, CancellationToken cancellationToken = default)
        where TOut : class, IHasId<RecipeId>
        => FindInternalAsync<TOut>(id, cancellationToken);

    public Task<TOut> FindAsync<TOut>(MealId id, CancellationToken cancellationToken = default)
        where TOut : class, IHasId<MealId>
        => FindInternalAsync<TOut>(id, cancellationToken);

    private async Task DeleteInternalAsync<TIn>(object idToDelete, CancellationToken cancellationToken)
        where TIn : class
    {
        // All current callers delete an entity referenced by an ID that was just supplied by the
        // caller (e.g. a DTO from a previous lookup); a missing entity here means data has already
        // become inconsistent, so this is a genuine error rather than an expected "not found" outcome.
        var entityToDelete = await dbContext.FindAsync<TIn>([idToDelete], cancellationToken)
            ?? throw new KeyNotFoundException($"No {typeof(TIn).Name} entity found for key '{idToDelete}'.");
        dbContext.Remove(entityToDelete);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<TOut> FindInternalAsync<TOut>(object id, CancellationToken cancellationToken)
        where TOut : class
    {
        // All current callers use the result unconditionally (no null-check), assuming the referenced
        // entity exists; throwing here surfaces a meaningful error instead of a NullReferenceException
        // further down the call stack.
        return await dbContext.Set<TOut>().FindAsync([id], cancellationToken)
            ?? throw new KeyNotFoundException($"No {typeof(TOut).Name} entity found for key '{id}'.");
    }
}
