namespace Sterling.NIPOutwardService.Data.Repositories.Implementations.TransactionAmountLimits;

public class EFTTransactionAmountLimitRepository : IEFTTransactionAmountLimitRepository
{
    private SqlDbContext dbContext;
    public EFTTransactionAmountLimitRepository(SqlDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<EFTTransactionAmountLimit> GetFirstRecord()
    {
        return await dbContext.EFTTransactionAmountLimit
        .Where(a => a.StatusFlag == 1)
        .FirstOrDefaultAsync();
    }
}