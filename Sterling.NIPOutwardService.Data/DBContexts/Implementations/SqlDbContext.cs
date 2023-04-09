namespace Sterling.NIPOutwardService.Data.DBContexts.Implementations;

public class SqlDbContext : DbContext 
{
    public SqlDbContext (DbContextOptions<SqlDbContext> options)
         : base(options)
    {
        
    }

    public virtual DbSet<NIPOutwardTransaction> tbl_NIPOutwardTransactions { get; set; }
    public virtual DbSet<NPOutwardNameEnquiry> tbl_NIPOutwardNameEnquiry { get; set; }
    public virtual DbSet<NIPOutwardDebitLookup> tbl_NIPOutwardDebitLookup { get; set; }
    public virtual DbSet<CBNTransactionAmountLimit> CBNTransactionAmountLimit { get; set; }
    public virtual DbSet<ConcessionTransactionAmountLimit> ConcessionTransactionAmountLimit { get; set; }
    public virtual DbSet<EFTTransactionAmountLimit> EFTTransactionAmountLimit { get; set; }
}