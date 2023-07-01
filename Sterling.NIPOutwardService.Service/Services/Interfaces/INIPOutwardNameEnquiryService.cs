using Sterling.NIPOutwardService.Domain.DataTransferObjects.Dtos.NameEnquiry;

namespace Sterling.NIPOutwardService.Service.Services.Interfaces;

public interface INIPOutwardNameEnquiryService 
{
    Task<Result<NameEnquiryResponseDto>> DoNameEnquiry(NameEnquiryRequestDto request);
    Task<Result<NameEnquiryResponseDto>> NameEnquiry(NameEnquiryRequestDto request);
    List<OutboundLog> GetOutboundLogs();
}