namespace Sterling.NIPOutwardService.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddDataDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        // MongoDB dependencies
        services.AddSingleton<IMongoClient, MongoClient>(sp => new MongoClient(configuration.GetSection("MongoDbSettings:ConnectionString").Value));
        services.AddSingleton<IMongoDbConfig, MongoDbConfig>(
            sp => new MongoDbConfig(configuration.GetSection("MongoDbSettings:ConnectionString").Value,
        configuration.GetSection("MongoDbSettings:DatabaseName").Value));
        services.AddScoped<IMongoDbContext, MongoDbContext>();

        // SQL Server dependencies
        services.AddDbContext<SqlDbContext>(options => 
        options.UseSqlServer(configuration.GetSection("AppSettings:SqlServerDbConnectionString").Value,
        builder =>
        {
            builder.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
        }));

        services.AddScoped<IInboundLogRepository, InboundLogRepository>();
        services.AddScoped<INIPOutwardTransactionRepository, NIPOutwardTransactionRepository>();
        services.AddScoped<INIPOutwardDebitLookupRepository, NIPOutwardDebitLookupRepository>();
        services.AddScoped<IDebitAccountRepository, DebitAccountRepository>();
        services.AddScoped<IIncomeAccountRepository, IncomeAccountRepository>();
        services.AddScoped<ITransactionDetailsRepository, TransactionDetailsRepository>();
        services.AddScoped<IUtilityHelper, UtilityHelper>();

        services.AddScoped<ICBNTransactionAmountLimitRepository, CBNTransactionAmountLimitRepository>();
        services.AddScoped<IConcessionTransactionAmountLimitRepository, ConcessionTransactionAmountLimitRepository>();
        services.AddScoped<IEFTTransactionAmountLimitRepository, EFTTransactionAmountLimitRepository>();
        
        return services;

    }

}