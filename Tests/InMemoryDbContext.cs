using DataLayer.EF;
using Microsoft.EntityFrameworkCore;

namespace Tests;

public class InMemoryDbContext : EfCoreContext
{
    public InMemoryDbContext()
        : base(new DbContextOptionsBuilder<EfCoreContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options)
    {
    }
}