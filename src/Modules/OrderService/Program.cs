using Barakah.EventBus;
using Barakah.OrderService.Data;
using Barakah.OrderService.Services;
using Barakah.TenantContext;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddHttpClient<ITenantResolver, HttpTenantResolver>(client =>
{
    var tenantServiceUrl = builder.Configuration["Services:TenantService"]
        ?? throw new InvalidOperationException("Missing Services:TenantService configuration.");
    client.BaseAddress = new Uri(tenantServiceUrl);
});
builder.Services.AddHttpClient<CatalogClient>(client =>
{
    var catalogServiceUrl = builder.Configuration["Services:CatalogService"]
        ?? throw new InvalidOperationException("Missing Services:CatalogService configuration.");
    client.BaseAddress = new Uri(catalogServiceUrl);
});
builder.Services.AddHttpClient<InventoryClient>(client =>
{
    var inventoryServiceUrl = builder.Configuration["Services:InventoryService"]
        ?? throw new InvalidOperationException("Missing Services:InventoryService configuration.");
    client.BaseAddress = new Uri(inventoryServiceUrl);
});

var kafkaOptions = builder.Configuration.GetSection("Kafka").Get<KafkaEventBusOptions>()
    ?? throw new InvalidOperationException("Missing Kafka configuration section.");
builder.Services.AddSingleton(kafkaOptions);
builder.Services.AddSingleton<IEventBus, KafkaEventBus>();

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
