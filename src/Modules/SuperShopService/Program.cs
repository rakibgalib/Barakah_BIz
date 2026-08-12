using Barakah.SuperShopService;
using Barakah.SuperShopService.Data;
using Barakah.TenantContext;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<SuperShopDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddHttpClient<ITenantResolver, HttpTenantResolver>(client =>
{
    var tenantServiceUrl = builder.Configuration["Services:TenantService"]
        ?? throw new InvalidOperationException("Missing Services:TenantService configuration.");
    client.BaseAddress = new Uri(tenantServiceUrl);
});

builder.Services.Configure<SuperShopOptions>(builder.Configuration.GetSection("SuperShopExtension"));

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
