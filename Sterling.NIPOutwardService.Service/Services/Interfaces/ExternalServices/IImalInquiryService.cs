using Sterling.NIPOutwardService.Domain.DataTransferObjects.Dtos.ImalTransaction;

namespace Sterling.NIPOutwardService.Service.Services.Interfaces.ExternalServices;

public interface IImalInquiryService 
{
    OutboundLog GetOutboundLog();
    Task<ImalGetAccountDetailsResponse> GetAccountDetailsByNuban(string nuban);
}