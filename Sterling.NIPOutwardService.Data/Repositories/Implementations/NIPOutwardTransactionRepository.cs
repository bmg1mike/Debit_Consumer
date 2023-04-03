using Sterling.NIPOutwardService.Data.Repositories.Interfaces;
using Sterling.NIPOutwardService.Domain.DataTransferObjects.Dtos;

namespace Sterling.NIPOutwardService.Data.Repositories.Implementations;

public partial class NIPOutwardTransactionRepository : INIPOutwardTransactionRepository
{
    private SqlDbContext dbContext;
    public NIPOutwardTransactionRepository(SqlDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task Create(NIPOutwardTransaction request)
    {
        await dbContext.tbl_NIPOutwardTransactions.AddAsync(request);
        await dbContext.SaveChangesAsync(); 
    }

    public async Task<int> Update(NIPOutwardTransaction request)
    {
        
        dbContext.tbl_NIPOutwardTransactions.Update(request);
        return await dbContext.SaveChangesAsync(); 
    }

    public async Task<bool> CheckIfTransactionIsSuccessful(string PaymentReference)
    {
        return await dbContext.tbl_NIPOutwardTransactions
        .Where(e => e.PaymentReference == PaymentReference && e.NIBSSResponse == "00")
        .AnyAsync();
    }

}
