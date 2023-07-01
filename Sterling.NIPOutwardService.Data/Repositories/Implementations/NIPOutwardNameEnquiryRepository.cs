namespace Sterling.NIPOutwardService.Data.Repositories.Implementations;

public class NIPOutwardNameEnquiryRepository:INIPOutwardNameEnquiryRepository
{
    private SqlDbContext dbContext;
    public NIPOutwardNameEnquiryRepository(SqlDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task Create(NIPOutwardNameEnquiry request)
    {
        await dbContext.tbl_NIPOutwardNameEnquiry.AddAsync(request);
        await dbContext.SaveChangesAsync(); 
    }

    public async Task<NIPOutwardNameEnquiry?> Get(string DestinationInstitutionCode, string AccountNumber)
    {
        return await dbContext.tbl_NIPOutwardNameEnquiry
        .Where(e => e.DestinationInstitutionCode == DestinationInstitutionCode 
        && e.AccountNumber == AccountNumber)
        .FirstOrDefaultAsync();
    }
}