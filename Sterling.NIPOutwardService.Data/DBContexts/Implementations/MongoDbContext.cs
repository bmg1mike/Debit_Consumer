namespace Sterling.NIPOutwardService.Data.DBContexts.Implementations;

public partial class MongoDbContext : IMongoDbContext
{
    public IMongoCollection<InboundLog> InboundLogs { get; set; }
    private readonly IConfiguration configuration;

    public MongoDbContext(IMongoDbConfig config, IMongoClient mongoClient, IConfiguration configuration)
    {
        this.configuration = configuration;
        var client = new MongoClient(config.ConnectionString);
        var database = client.GetDatabase(config.DatabaseName);

        InboundLogs = database.GetCollection<InboundLog>(configuration["MongoDbSettings:CollectionName"]);
        
    }

}
