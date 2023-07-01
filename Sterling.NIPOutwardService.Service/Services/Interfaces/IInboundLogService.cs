namespace Sterling.NIPOutwardService.Service.Services.Interfaces;

public partial interface IInboundLogService
{
    Task<FundsTransferResult<List<InboundLog>>> GetInboundLogs();
    Task<FundsTransferResult<InboundLog>>  GetInboundLog(string id);
    Task<FundsTransferResult<string>> CreateInboundLog(InboundLog inboundLog);
    Task<FundsTransferResult<bool>> UpdateInboundLog(string id, InboundLog inboundLog);
    Task<FundsTransferResult<bool>> RemoveInboundLog(string id);
}