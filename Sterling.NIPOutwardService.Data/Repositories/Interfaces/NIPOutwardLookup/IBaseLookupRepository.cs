namespace Sterling.NIPOutwardService.Data.Repositories.Interfaces.NIPOutwardLookup;

public interface IBaseLookupRepository<TEntity>
    where TEntity : BaseLookup 
{
    Task<bool> FindByNIPOutwardTransactionID (long ID);
    Task<int> Create(TEntity entity);
    Task<int> Delete(TEntity entity);    
}