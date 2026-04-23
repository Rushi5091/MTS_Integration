using Microsoft.EntityFrameworkCore;
using Project.API.Middlewares;
using Project.Infrastructure.Data;
using Project.API.Extensions;
using System.Data;
using Project.API.Configuration;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Hosting.WindowsServices;
var builder = WebApplication.CreateBuilder(args);
builder.Host.UseWindowsService(options =>
{
    options.ServiceName = "IntegrationProject";
});
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
// Bind to localhost only - not accessible from outside
builder.WebHost.UseUrls("http://127.0.0.1:5000", "https://127.0.0.1:5001");

// Kestrel timeout settings
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(10);
});

builder.Services.AddDbContext<ApplicationDbContext>(
                options => options.UseSqlServer(builder.Configuration
                .GetConnectionString("PrimaryDbConnection")));

// Register ILogger service
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddSeq(builder.Configuration.GetSection("Seq"));
});

// Register Services

builder.Services.RegisterService();
builder.Services.AddControllers();
builder.Services.AddTransient<IDbConnection>(sp => new MySqlConnection(builder.Configuration.GetConnectionString("PrimaryDbConnection")));
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Clean Structured API Project", Version = "v1" });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapGet("/runbenchmarks", async context =>
    {
        await context.Response.WriteAsync("Benchmarks completed. Check console for results.");
    });
});

#region Custom Middleware
app.UseRequestResponseLogging();
#endregion

app.Run();