namespace Sterling.NIPOutwardService.Data.Repositories.Interfaces;

public partial interface INIPOutwardTransactionRepository 
{
    Task Create(NIPOutwardTransaction request);
    Task<int> Update(NIPOutwardTransaction request);
    Task<NIPOutwardTransaction?> GetBySessionID(string SessionID);
}