namespace Sterling.NIPOutwardService.Service.Services.Interfaces;

public interface IVtellerService
{
    Task<VTellerResponseDto> authorizeIBSTrnxFromSterling(CreateVTellerTransactionDto t,
     IncomeAccountsDetails incomeAccountsDetails);
    List<OutboundLog> GetOutboundLogs();
}