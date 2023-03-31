using Sterling.NIPOutwardService.Domain.Common.Generics;
using Sterling.NIPOutwardService.Domain.DataTransferObjects.Dtos.NameEnquiry;

namespace Sterling.NIPOutwardService.Service.Services.Interfaces;

public interface INIPOutwardNameEnquiryService 
{
    Task<NameEnquiryResult<NameEnquiryResponseDto>> DoNameEnquiry(NameEnquiryRequestDto request);
}