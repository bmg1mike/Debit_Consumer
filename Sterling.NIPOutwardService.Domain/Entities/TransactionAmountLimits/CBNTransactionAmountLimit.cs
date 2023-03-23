namespace Sterling.NIPOutwardService.Domain.Entities.TransactionAmountLimits;

[Table("tbl_nipcbnamt")]
[Keyless]
public class CBNTransactionAmountLimit 
{
    [Column("minamt")]
    public decimal MaximumAmountPerTransaction { get; set; } 
    [Column("maxamt")]
    public decimal MaximumAmountPerDay { get; set; } 
    [Column("cus_class")]
    public int CustomerClass { get; set; } 
     [Column("statusflag")]
    public int StatusFlag { get; set; } 
}