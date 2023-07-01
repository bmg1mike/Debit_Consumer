namespace Sterling.NIPOutwardService.Domain.Config.Interfaces;

public partial interface IMongoDbConfig
{
      string DatabaseName { get; set; }
      string ConnectionString { get; set; }

}