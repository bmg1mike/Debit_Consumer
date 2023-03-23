namespace Sterling.NIPOutwardService.Service;

public static class DependencyInjection
{
     public static IServiceCollection AddServiceDependencies(this IServiceCollection services, IConfiguration configuration)
    {

        services.AddScoped<INIPOutwardTransactionService, NIPOutwardTransactionService>();
        services.AddScoped<IInboundLogService, InboundLogService>();
        services.AddScoped<IFraudAnalyticsService, FraudAnalyticsService>();
        services.AddScoped<INIPOutwardDebitLookupService, NIPOutwardDebitLookupService>();
        services.AddScoped<INIPOutwardDebitService, NIPOutwardDebitService>();
        services.AddScoped<INIPOutwardTransactionService, NIPOutwardTransactionService>();
        services.AddScoped<ITransactionAmountLimitService, TransactionAmountLimitService>();
        services.AddScoped<IVtellerService, VTellerService>();
        

        services.AddHttpClient<IFraudAnalyticsService, FraudAnalyticsService>()
        .AddPolicyHandler(GetRetryPolicy());

        services.AddAutoMapper(typeof(AutoMapping));
        services.AddControllersWithViews();

        var appSettings = configuration.GetSection("AppSettings");
        services.Configure<AppSettings>(appSettings);

        string secretKey = configuration.GetSection("JwtConfig:Secret").Value;
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(x =>
            {
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration.GetSection("JwtConfig:Secret").Value)),
                    ValidateIssuer = false,
                    //ValidIssuer = configuration.GetSection("JwtConfig:Issuer").Value,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
        //x.Authority = configuration["Token_Issuer"];
    });

        return services;
    }

    public static IServiceCollection AddDebitProducerServiceDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        // ClientConfig? clientConfig = new ClientConfig();
        // clientConfig.BootstrapServers = configuration.GetSection("AppSettings:KafkaProducerConfig:BootstrapServers").Value;
        services.AddScoped<INIPOutwardDebitProducerService, NIPOutwardDebitProducerService>();
        ClientConfig kafkaConfig = configuration
        .GetSection("KafkaDebitProducerConfig:ClientConfig")
        .Get<ClientConfig>();

        services.AddSingleton<IProducer<Null, byte[]>> 
        (x => new ProducerBuilder<Null, byte[]>(kafkaConfig).Build()) ;

        var kafkaDebitProducerConfig = configuration.GetSection("KafkaDebitProducerConfig");
        services.Configure<KafkaDebitProducerConfig>(kafkaDebitProducerConfig);

        return services;
    }

    public static IServiceCollection AddDebitConsumerServiceDependencies(this IServiceCollection services, IConfiguration configuration)
    {        
        ConsumerConfig kafkaConfig = configuration
        .GetSection("KafkaDebitConsumerConfig:ClientConfig")
        .Get<ConsumerConfig>();
        kafkaConfig.AutoOffsetReset = AutoOffsetReset.Earliest;

        services.AddSingleton<IConsumer<Ignore, byte[]>> 
        (x => new ConsumerBuilder<Ignore, byte[]>(kafkaConfig).Build()) ;

        var kafkaDebitConsumerConfig = configuration.GetSection("KafkaDebitConsumerConfig");
        services.Configure<KafkaDebitConsumerConfig>(kafkaDebitConsumerConfig);

        services.AddScoped<NIPOutwardDebitService>();

        return services;
    }

    static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2,
                                                                        retryAttempt)));
    }
}