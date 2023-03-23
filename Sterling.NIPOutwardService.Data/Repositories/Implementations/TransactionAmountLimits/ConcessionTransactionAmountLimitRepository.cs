namespace Sterling.NIPOutwardService.Data.Repositories.Implementations.TransactionAmountLimits;

public class ConcessionTransactionAmountLimitRepository:IConcessionTransactionAmountLimitRepository 
{
    private SqlDbContext dbContext;
    public ConcessionTransactionAmountLimitRepository(SqlDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<ConcessionTransactionAmountLimit> GetByDebitAccount(string debitAccountNumber)
    {
        return await dbContext.ConcessionTransactionAmountLimit
        .Where(a => a.DebitAccountNumber == debitAccountNumber && a.StatusFlag == 1)
        .FirstOrDefaultAsync();
    }
}