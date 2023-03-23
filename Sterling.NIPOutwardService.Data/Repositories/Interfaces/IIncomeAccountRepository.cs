namespace Sterling.NIPOutwardService.Data.Repositories.Interfaces;

public interface IIncomeAccountRepository 
{
    Task <string?> GetExpcode();
    Task<string?> GetCurrentTss();
    Task<Fee?> GetCurrentIncomeAcct();
    Task<string?> getCurrentTss2();
    OutboundLog GetOutboundLog();
}