namespace Sterling.NIPOutwardService.Domain.Entities.NIPOutwardLookup;

public class BaseLookup 
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public long ID { get; set; }
    public long NIPOutwardTransactionID { get; set; }
    public int StatusFlag { get; set; }
    public DateTime DateProcessed { get; set; }
}