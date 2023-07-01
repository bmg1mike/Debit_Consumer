namespace Sterling.NIPOutwardService.Data.DBContexts.Interfaces;

public partial interface IMongoDbContext
{
    IMongoCollection<InboundLog> InboundLogs { get; set; }

}