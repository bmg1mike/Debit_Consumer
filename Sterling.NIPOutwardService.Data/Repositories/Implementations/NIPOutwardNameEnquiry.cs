namespace Sterling.NIPOutwardService.Data.Repositories.Implementations;

public class NIPOutwardNameEnquiry:INIPOutwardNameEnquiry
{
    private SqlDbContext dbContext;
    public NIPOutwardNameEnquiry(SqlDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task Create(NPOutwardNameEnquiry request)
    {
        await dbContext.tbl_NIPOutwardNameEnquiry.AddAsync(request);
        await dbContext.SaveChangesAsync(); 
    }

    public async Task<NPOutwardNameEnquiry?> Get(string DestinationInstitutionCode, string AccountNumber)
    {
        return await dbContext.tbl_NIPOutwardNameEnquiry
        .Where(e => e.DestinationInstitutionCode == DestinationInstitutionCode 
        && e.AccountNumber == AccountNumber)
        .FirstOrDefaultAsync();
    }
}