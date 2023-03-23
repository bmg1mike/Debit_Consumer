namespace Sterling.NIPOutwardService.Data.Repositories.Interfaces;

public interface ITransactionDetailsRepository 
{
    Task<string> GenerateNameEnquirySessionId(string OldSessionId);
    Task<NIPOutwardCharges> GetNIPFee(decimal amt);
    Task<TotalTransactionDonePerDay> GetTotalTransDonePerday(decimal Maxperday, decimal amt, string nuban);
    Task<bool> isDateHoliday(DateTime dt);
    Task<bool> isBankCodeFound(string bankCode);
    Task<bool> isLedgerNotAllowed(string ledgerCode);
    Task<bool> isLedgerFound(string led_code);
    Task<FreeFeeAccount?> GetFreeFeeAccount(string accountNo);
    OutboundLog GetOutboundLog();
}