namespace Sterling.NIPOutwardService.Domain.Entities.TransactionAmountLimits;

[Table("tbl_savingsLecodeEFTAmt")]
[Keyless]
public class EFTTransactionAmountLimit
{
    [Column("maxpertrans")]
    public decimal MaximumAmountPerTransaction { get; set; } 
    [Column("maxperday")]
    public decimal MaximumAmountPerDay { get; set; } 
    [Column("statusflag")]
    public int StatusFlag { get; set; } 
}