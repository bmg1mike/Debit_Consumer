namespace Sterling.NIPOutwardService.Data.Repositories.Interfaces;

public interface INIPOutwardNameEnquiryRepository
{
    Task Create(NIPOutwardNameEnquiry request);
    Task<NIPOutwardNameEnquiry?> Get(string DestinationInstitutionCode, string AccountNumber);
}