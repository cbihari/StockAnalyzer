using Microsoft.OpenApi.Models;
using StockAnalyzer.Api.Middleware;
using StockAnalyzer.Application;
using StockAnalyzer.Infrastructure;
using StockAnalyzer.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "StockAnalyzer API",
        Version = "v1",
        Description = "Educational stock-analysis gateway. Predictions are not financial advice.",
        Contact = new OpenApiContact { Name = "StockAnalyzer contributors" }
    });
    var xmlPath = Path.Combine(AppContext.BaseDirectory, "StockAnalyzer.Api.xml");
    options.IncludeXmlComments(xmlPath);
});
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
var allowedOrigins = (Environment.GetEnvironmentVariable("ALLOWED_ORIGINS")
        ?? builder.Configuration["FrontendUrl"]
        ?? "http://localhost:4200")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod());
});
var app = builder.Build();

await app.Services.MigrateDatabaseAsync();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI(options =>
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "StockAnalyzer API v1"));

app.UseCors("Frontend");
app.UseAuthorization();

app.MapControllers();

app.Run();
