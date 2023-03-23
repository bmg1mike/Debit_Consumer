namespace Sterling.NIPOutwardService.Domain.Entities.TransactionAmountLimits;

[Table("tbl_nipconcessionTrnxlimits")]
[Keyless]
public class ConcessionTransactionAmountLimit 
{
    [Column("maxpertran")]
    public decimal MaximumAmountPerTransaction { get; set; } 
    [Column("maxperday")]
    public decimal MaximumAmountPerDay { get; set; } 
    [Column("statusflag")]
    public int StatusFlag { get; set; } 
    [Column("nuban")]
    public string DebitAccountNumber { get; set; } 
}