namespace Sterling.NIPOutwardService.Data.Repositories.Implementations.NIPOutwardLookup;

public class BaseLookupRepository<TEntity> : IBaseLookupRepository<TEntity>
    where TEntity : BaseLookup 
{
    private SqlDbContext dbContext;
    public BaseLookupRepository(SqlDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<int> Create(TEntity entity)
    {
        await dbContext.Set<TEntity>().AddAsync(entity);
        return await dbContext.SaveChangesAsync(); 
    }

    public async Task<bool> FindByNIPOutwardTransactionID (long ID)
    {
        return await dbContext.Set<TEntity>().Where(e => e.NIPOutwardTransactionID == ID).AnyAsync();
    }

    public async Task<int> Delete(TEntity entity)
    {
        dbContext.Set<TEntity>().Remove(entity);
        return await dbContext.SaveChangesAsync(); 
    }
}