using System.Net;
using Confluent.Kafka;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Host.UseSerilog((context, config) => 
{
    config.Enrich.FromLogContext()
        .WriteTo.Console()
        .ReadFrom.Configuration(context.Configuration);
    
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(
    c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Sterling Outward Funds Transfer Service", Version = "v1" });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "Jwt auth header",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        },
                        Scheme = "oauth2",
                        Name = "Bearer",
                        In = ParameterLocation.Header
                    },
                    new List<string>()
                }
            });
        }
);

ProducerConfig kafkaDebitProducerConfig = builder.Configuration
        .GetSection("KafkaDebitProducerConfig:ClientConfig")
        .Get<ProducerConfig>();

ProducerConfig kafkaSendToNIBSSProducerConfig = builder.Configuration
        .GetSection("KafkaSendToNIBSSProducerConfig:ClientConfig")
        .Get<ProducerConfig>();
        

builder.Services.AddHealthChecks()
   .AddUrlGroup(new Uri    
            ("https://www.google.com"),
             name: "Internet Connectivity",
             failureStatus: HealthStatus.Degraded)
    .AddUrlGroup(new Uri    
            (builder.Configuration.GetSection("AppSettings:FraudBaseUrl").Value),
             name: "Fraud API",
             failureStatus: HealthStatus.Degraded)
    .AddUrlGroup(new Uri    
            (builder.Configuration.GetSection("AppSettings:VtellerProperties:BaseUrl").Value),
             name: "VTeller",
             failureStatus: HealthStatus.Degraded)
    .AddSqlServer(
             builder.Configuration.GetSection("AppSettings:SqlServerDbConnectionString").Value, 
             name: "Sql Server Database",
             failureStatus: HealthStatus.Degraded)
    .AddMongoDb(builder.Configuration.GetSection("MongoDbSettings:ConnectionString").Value,
            name: "Mongo Database",
            failureStatus: HealthStatus.Degraded)
    .AddOracle(builder.Configuration.GetSection("AppSettings:T24DbConnectionString").Value,
            name: "T24 Oracle Database",
            failureStatus: HealthStatus.Degraded)
    .AddKafka(kafkaDebitProducerConfig,
            name: "Kafka Debit Producer",
            failureStatus: HealthStatus.Degraded)
    .AddKafka(kafkaSendToNIBSSProducerConfig,
            name: "Kafka Send To NIBSS Producer",
            failureStatus: HealthStatus.Degraded)
    .AddUrlGroup(new Uri    
            (builder.Configuration.GetSection("AppSettings:NibssNipServiceProperties:NIPNIBSSService").Value),
             name: "NIBSS Web Service",
             failureStatus: HealthStatus.Degraded);

builder.Services.AddCors(p => p.AddPolicy("corsapp", builder =>
    {
        builder.WithOrigins("*").AllowAnyMethod().AllowAnyHeader();
    }));

builder.Services.AddDataDependencies(builder.Configuration);
builder.Services.AddServiceDependencies(builder.Configuration);
builder.Services.AddDebitProducerServiceDependencies(builder.Configuration);
builder.Services.AddSendToNIBBSProducerServiceDependencies(builder.Configuration);

builder.Services.AddApiVersioning(x =>  
            {  
                x.DefaultApiVersion = new ApiVersion(1, 0);  
                x.AssumeDefaultVersionWhenUnspecified = true;
                x.ReportApiVersions = true;  
                //x.ApiVersionReader = new HeaderApiVersionReader("x-api-version");  
            });  

builder.Services.AddHealthChecksUI(opt =>    
{    
    opt.SetEvaluationTimeInSeconds(60); //time in seconds between check    
    opt.MaximumHistoryEntriesPerEndpoint(60); //maximum history of checks    
    opt.SetApiMaxActiveRequests(1); //api requests concurrency    
    opt.AddHealthCheckEndpoint("default api", "/health"); //map health check api    
})    
.AddInMemoryStorage();  

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}

app.UseSerilogRequestLogging();

app.UseCors("corsapp");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseHealthChecksUI();

app.MapHealthChecks("/health", new HealthCheckOptions()
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });

app.Run();
