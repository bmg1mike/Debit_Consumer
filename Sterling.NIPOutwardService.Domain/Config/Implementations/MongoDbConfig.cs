namespace Sterling.NIPOutwardService.Domain.Config.Implementations;

public partial class MongoDbConfig:IMongoDbConfig
{

    public string DatabaseName { get; set; }
    public string ConnectionString { get; set; }
    public MongoDbConfig(string connectionString, string databaseName)
    {
        DatabaseName = databaseName;
        ConnectionString =  connectionString;

    }   
}