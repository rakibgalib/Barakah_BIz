using Barakah.EventBus;
using Barakah.NotificationService.Consumers;
using Barakah.NotificationService.Data;
using Barakah.TenantContext;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddHttpClient<ITenantResolver, HttpTenantResolver>(client =>
{
    var tenantServiceUrl = builder.Configuration["Services:TenantService"]
        ?? throw new InvalidOperationException("Missing Services:TenantService configuration.");
    client.BaseAddress = new Uri(tenantServiceUrl);
});

var kafkaConsumerOptions = builder.Configuration.GetSection("Kafka").Get<KafkaConsumerOptions>()
    ?? throw new InvalidOperationException("Missing Kafka configuration section.");
builder.Services.AddSingleton(kafkaConsumerOptions);
builder.Services.AddSingleton<IEventSubscriber, KafkaEventSubscriber>();
builder.Services.AddHostedService<OrderCreatedConsumer>();

builder.Services.AddHealthChecks();

builder.Services.AddCors(options => options.AddPolicy("AdminDashboard", policy =>
    policy.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AdminDashboard");
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
