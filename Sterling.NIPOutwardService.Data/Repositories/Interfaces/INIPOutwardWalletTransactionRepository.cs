namespace Sterling.NIPOutwardService.Data.Repositories.Interfaces;

public interface INIPOutwardWalletTransactionRepository 
{
    Task Create(NIPOutwardWalletTransaction request);
    Task<int> Update(NIPOutwardWalletTransaction request);
}