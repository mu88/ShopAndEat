using AutoMapper;
using DataLayer.EF;

namespace ServiceLayer.Concrete;

public class SimpleCrudHelper(EfCoreContext dbContext, IMapper mapper)
{
    public TDtoOut Create<TDtoIn, TIn, TDtoOut>(TDtoIn newDto)
    {
        var mappedObject = mapper.Map<TDtoIn, TIn>(newDto);
        var addedEntity = dbContext.Add(mappedObject);
        dbContext.SaveChanges();

        return mapper.Map<TDtoOut>(addedEntity.Entity);
    }

    public void Delete<TIn>(int idToDelete)
        where TIn : class
    {
        var entityToDelete = dbContext.Find<TIn>(idToDelete);
        dbContext.Remove(entityToDelete);
        dbContext.SaveChanges();
    }

    public IEnumerable<TDtoOut> GetAllAsDto<TIn, TDtoOut>()
        where TIn : class
    {
        var allEntities = dbContext.Set<TIn>();

        return mapper.Map<IEnumerable<TDtoOut>>(allEntities);
    }

    public IEnumerable<TOut> FindMany<TOut>(IEnumerable<int> ids)
        where TOut : class
    {
        return ids.Select(id => dbContext.Set<TOut>().Find(id));
    }

    public TOut Find<TOut>(int id)
        where TOut : class
    {
        return dbContext.Set<TOut>().Find(id);
    }
}