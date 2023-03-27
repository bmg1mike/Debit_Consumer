
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) => 
{
    config.Enrich.FromLogContext()
        .WriteTo.Console()
        .ReadFrom.Configuration(context.Configuration);
    
});

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddHostedService<ConsumerBackgroundWorkerService>();
builder.Services.AddDataDependencies(builder.Configuration);
builder.Services.AddServiceDependencies(builder.Configuration);
builder.Services.AddDebitConsumerServiceDependencies(builder.Configuration);
builder.Services.AddSendToNIBBSProducerServiceDependencies(builder.Configuration);
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
