using DataLayer.EF;

namespace ServiceLayer.Concrete;

public class SimpleCrudHelper(EfCoreContext dbContext)
{
    public TDtoOut Create<TDtoIn, TIn, TDtoOut>(TDtoIn newDto, Func<TDtoIn, TIn> toEntity, Func<TIn, TDtoOut> toDto)
        where TIn : class
    {
        var addedEntity = dbContext.Add(toEntity(newDto));
        dbContext.SaveChanges();

        return toDto(addedEntity.Entity);
    }

    public void Delete<TIn>(int idToDelete)
        where TIn : class
    {
        var entityToDelete = dbContext.Find<TIn>(idToDelete);
        dbContext.Remove(entityToDelete);
        dbContext.SaveChanges();
    }

    public IEnumerable<TDtoOut> GetAllAsDto<TIn, TDtoOut>(Func<TIn, TDtoOut> toDto)
        where TIn : class
        => dbContext.Set<TIn>().AsEnumerable().Select(toDto);

    public IEnumerable<TOut> FindMany<TOut>(IEnumerable<int> ids)
        where TOut : class
        => ids.Select(id => dbContext.Set<TOut>().Find(id));

    public TOut Find<TOut>(int id)
        where TOut : class
        => dbContext.Set<TOut>().Find(id);
}
