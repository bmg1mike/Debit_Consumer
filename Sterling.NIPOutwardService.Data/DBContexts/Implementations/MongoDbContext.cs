namespace Sterling.NIPOutwardService.Data.DBContexts.Implementations;

public partial class MongoDbContext : IMongoDbContext
{
    public IMongoCollection<InboundLog> InboundLogs { get; set; }
    
    public MongoDbContext(IMongoDbConfig config,IMongoClient mongoClient)
    {
        var client = new MongoClient(config.ConnectionString);
        var database = client.GetDatabase(config.DatabaseName);

        InboundLogs = database.GetCollection<InboundLog>("InboundLogs");
    }

}
