namespace Sterling.NIPOutwardService.Data.Repositories.Implementations;

public partial class NIPOutwardWalletTransactionRepository : INIPOutwardWalletTransactionRepository
{
    private SqlDbContext dbContext;
    public NIPOutwardWalletTransactionRepository(SqlDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task Create(NIPOutwardWalletTransaction request)
    {
        await dbContext.tbl_NIPOutwardWalletTransactions.AddAsync(request);
        await dbContext.SaveChangesAsync(); 
    }

    public async Task<int> Update(NIPOutwardWalletTransaction request)
    {
        
        dbContext.tbl_NIPOutwardWalletTransactions.Update(request);
        return await dbContext.SaveChangesAsync(); 
    }

}