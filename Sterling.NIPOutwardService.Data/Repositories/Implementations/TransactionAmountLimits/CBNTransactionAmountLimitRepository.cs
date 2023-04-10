namespace Sterling.NIPOutwardService.Data.Repositories.Implementations.TransactionAmountLimits;

public class CBNTransactionAmountLimitRepository:ICBNTransactionAmountLimitRepository 
{
    private SqlDbContext dbContext;
    public CBNTransactionAmountLimitRepository(SqlDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<CBNTransactionAmountLimit?> GetByCustomerClass(int customerClass)
    {
        return await dbContext.CBNTransactionAmountLimit.Where(a => a.CustomerClass == customerClass && a.StatusFlag == 1).FirstOrDefaultAsync();
    }
}