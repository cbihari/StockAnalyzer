using System.Text;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StockAnalyzer.Api.Auth;
using StockAnalyzer.Api.Middleware;
using StockAnalyzer.Application;
using StockAnalyzer.Infrastructure;
using StockAnalyzer.Infrastructure.Persistence;
using StockAnalyzer.Infrastructure.Identity;

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
builder.Services.AddIdentityCore<StockAnalyzerUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<StockAnalyzerDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddHttpContextAccessor();
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? builder.Configuration["Jwt:Secret"]
    ?? (builder.Environment.IsDevelopment()
        ? "local-development-only-change-this-jwt-secret"
        : throw new InvalidOperationException("JWT_SECRET is required outside development."));
var googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID")
    ?? builder.Configuration["Google:ClientId"];
var googleClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET")
    ?? builder.Configuration["Google:ClientSecret"];
var googleEnabled = IsConfiguredSecret(googleClientId) && IsConfiguredSecret(googleClientSecret);
builder.Services.AddSingleton(new AuthProviderOptions(googleEnabled));
var authentication = builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "StockAnalyzer",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "StockAnalyzer.Web",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    })
    .AddCookie(IdentityConstants.ExternalScheme);
if (googleEnabled)
{
    authentication.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
    {
        options.SignInScheme = IdentityConstants.ExternalScheme;
        options.ClientId = googleClientId!;
        options.ClientSecret = googleClientSecret!;
    });
}
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

var forwardedHeaders = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeaders.KnownNetworks.Clear();
forwardedHeaders.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeaders);
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI(options =>
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "StockAnalyzer API v1"));

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static bool IsConfiguredSecret(string? value) =>
    !string.IsNullOrWhiteSpace(value) && !value.StartsWith("placeholder-", StringComparison.OrdinalIgnoreCase);
