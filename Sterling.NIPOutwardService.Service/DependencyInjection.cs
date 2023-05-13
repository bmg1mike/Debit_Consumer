using Microsoft.Extensions.Caching.Memory;
using Sterling.NIPOutwardService.Service.Services.Implementations.ExternalServices;

namespace Sterling.NIPOutwardService.Service;

public static class DependencyInjection
{
     public static IServiceCollection AddServiceDependencies(this IServiceCollection services, IConfiguration configuration)
    {

        services.AddScoped<INIPOutwardTransactionService, NIPOutwardTransactionService>();
        services.AddScoped<IInboundLogService, InboundLogService>();
        services.AddScoped<IFraudAnalyticsService, FraudAnalyticsService>();
        services.AddScoped<INIPOutwardDebitLookupService, NIPOutwardDebitLookupService>();
        services.AddScoped<INIPOutwardSendToNIBSSProducerService, NIPOutwardSendToNIBSSProducerService>();
        services.AddScoped<INIPOutwardDebitProcessorService, NIPOutwardDebitProcessorService>();
        services.AddScoped<INIPOutwardTransactionService, NIPOutwardTransactionService>();
        services.AddScoped<ITransactionAmountLimitService, TransactionAmountLimitService>();
        services.AddScoped<IEncryption, Encryption>();
        services.AddScoped<INIPOutwardWalletTransactionService, NIPOutwardWalletTransactionService>();
        services.AddScoped<IWalletFraudAnalyticsService, WalletFraudAnalyticsService>();
        services.AddScoped<INIPOutwardWalletTransactionRepository, NIPOutwardWalletTransactionRepository>();
        services.AddScoped<IWalletTransactionService, WalletTransactionService>();
        services.AddScoped<IImalInquiryService, ImalInquiryService>();
        services.AddScoped<IImalTransactionService, ImalTransactionService>();
        services.AddScoped<INIPOutwardImalDebitProcessorService, NIPOutwardImalDebitProcessorService>();
        services.AddSingleton<IMemoryCache, MemoryCache>();
        

        services.AddHttpClient<IFraudAnalyticsService, FraudAnalyticsService>()
        .AddPolicyHandler(GetRetryPolicy());

        services.AddHttpClient<IVtellerService, VTellerService>();

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

        services.AddScoped<INIPOutwardNameEnquiryService, NIPOutwardNameEnquiryService>();
        services.AddScoped<INIPOutwardNameEnquiryRepository, NIPOutwardNameEnquiryRepository>();
        services.AddScoped<ISSM, SSM>();
        services.AddHttpContextAccessor();

        var NibssNipServiceProperties = configuration.GetSection("APPSettings:NibssNipServiceProperties");
        services.Configure<NibssNipServiceProperties>(NibssNipServiceProperties);

        return services;
    }

    public static IServiceCollection AddDebitProducerServiceDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        // ClientConfig? clientConfig = new ClientConfig();
        // clientConfig.BootstrapServers = configuration.GetSection("AppSettings:KafkaProducerConfig:BootstrapServers").Value;
        services.AddScoped<INIPOutwardDebitProducerService, NIPOutwardDebitProducerService>();
        services.AddScoped<INIPOutwardDebitService, NIPOutwardDebitService>();
        ClientConfig kafkaConfig = configuration
        .GetSection("KafkaDebitProducerConfig:ClientConfig")
        .Get<ClientConfig>();

        services.AddSingleton<IProducer<Null, string>> 
        (x => new ProducerBuilder<Null, string>(kafkaConfig).Build()) ;

        var kafkaDebitProducerConfig = configuration.GetSection("KafkaDebitProducerConfig");
        services.Configure<KafkaDebitProducerConfig>(kafkaDebitProducerConfig);

        return services;
    }

    public static IServiceCollection AddDebitConsumerServiceDependencies(this IServiceCollection services, IConfiguration configuration)
    {        
        ConsumerConfig kafkaConfig = configuration
        .GetSection("KafkaDebitConsumerConfig:ConsumerConfig")
        .Get<ConsumerConfig>();
        kafkaConfig.AutoOffsetReset = AutoOffsetReset.Earliest;

        services.AddSingleton<IConsumer<Ignore, string>> 
        (x => new ConsumerBuilder<Ignore, string>(kafkaConfig).Build()) ;

        var kafkaDebitConsumerConfig = configuration.GetSection("KafkaDebitConsumerConfig");
        services.Configure<KafkaDebitConsumerConfig>(kafkaDebitConsumerConfig);

        services.AddScoped<NIPOutwardDebitProcessorService>();

        return services;
    }

    public static IServiceCollection AddSendToNIBBSProducerServiceDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<INIPOutwardSendToNIBSSProducerService, NIPOutwardSendToNIBSSProducerService>();
        ClientConfig kafkaConfig = configuration
        .GetSection("KafkaSendToNIBSSProducerConfig:ClientConfig")
        .Get<ClientConfig>();

        services.AddSingleton<IProducer<Null, string>> 
        (x => new ProducerBuilder<Null, string>(kafkaConfig).Build()) ;

        var kafkaSendToNIBSSProducerConfig = configuration.GetSection("KafkaSendToNIBSSProducerConfig");
        services.Configure<KafkaSendToNIBSSProducerConfig>(kafkaSendToNIBSSProducerConfig);

        var kafkaImalSendToNIBSSProducerConfig = configuration.GetSection("KafkaImalSendToNIBSSProducerConfig");
        services.Configure<KafkaImalSendToNIBSSProducerConfig>(kafkaImalSendToNIBSSProducerConfig);

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