namespace Sterling.NIPOutwardService.Service.Services.Interfaces;

public partial interface IInboundLogService
{
    Task<Result<List<InboundLog>>> GetInboundLogs();
    Task<Result<InboundLog>>  GetInboundLog(string id);
    Task<Result<string>> CreateInboundLog(InboundLog inboundLog);
    Task<Result<bool>> UpdateInboundLog(string id, InboundLog inboundLog);
    Task<Result<bool>> RemoveInboundLog(string id);
}