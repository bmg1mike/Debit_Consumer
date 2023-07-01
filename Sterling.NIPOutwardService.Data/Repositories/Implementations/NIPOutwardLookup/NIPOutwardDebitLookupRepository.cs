using Sterling.NIPOutwardService.Data.Repositories.Interfaces.NIPOutwardLookup;

namespace Sterling.NIPOutwardService.Data.Repositories.Implementations.NIPOutwardLookup;

public class NIPOutwardDebitLookupRepository : BaseLookupRepository<NIPOutwardDebitLookup>, INIPOutwardDebitLookupRepository
{
    public NIPOutwardDebitLookupRepository(SqlDbContext dbContext) : base(dbContext)
    {
        
    }
}