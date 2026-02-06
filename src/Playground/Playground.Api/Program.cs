using FSH.Framework.Web;
using FSH.Framework.Web.Modules;
using FSH.Modules.Auditing;
using FSH.Modules.Identity;
using FSH.Modules.Identity.Contracts.v1.Tokens.TokenGeneration;
using FSH.Modules.Identity.Features.v1.Tokens.TokenGeneration;
using FSH.Modules.Messaging;
using FSH.Modules.Multitenancy;
using FSH.Modules.Multitenancy.Contracts.v1.GetTenantStatus;
using FSH.Modules.Multitenancy.Features.v1.GetTenantStatus;
using FSH.Playground.Api.Endpoints.v1.ExternalGateway;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Map Aspire connection strings to application configuration keys
// Aspire injects: ConnectionStrings__fsh, ConnectionStrings__redis, and ConnectionStrings__rabbitmq
// Application expects: DatabaseOptions__ConnectionString, CachingOptions__Redis, and RabbitMqOptions__ConnectionString
var postgresConnectionString = builder.Configuration.GetConnectionString("fsh");
var redisConnectionString = builder.Configuration.GetConnectionString("redis");
var rabbitmqConnectionString = builder.Configuration.GetConnectionString("rabbitmq");

if (!string.IsNullOrEmpty(postgresConnectionString))
{
    builder.Configuration["DatabaseOptions:ConnectionString"] = postgresConnectionString;
}

if (!string.IsNullOrEmpty(redisConnectionString))
{
    builder.Configuration["CachingOptions:Redis"] = redisConnectionString;
}

if (!string.IsNullOrEmpty(rabbitmqConnectionString))
{
    builder.Configuration["RabbitMqOptions:ConnectionString"] = rabbitmqConnectionString;
}

if (builder.Environment.IsProduction())
{
    static void Require(IConfiguration config, string key)
    {
        if (string.IsNullOrWhiteSpace(config[key]))
        {
            throw new InvalidOperationException($"Missing required configuration '{key}' in Production.");
        }
    }

    var config = builder.Configuration;
    Require(config, "DatabaseOptions:ConnectionString");
    Require(config, "CachingOptions:Redis");
    Require(config, "JwtOptions:SigningKey");
}

builder.Services.AddMediator(o =>
{
    o.ServiceLifetime = ServiceLifetime.Scoped;
    o.Assemblies = [
        typeof(GenerateTokenCommand),
        typeof(GenerateTokenCommandHandler),
        typeof(GetTenantStatusQuery),
        typeof(GetTenantStatusQueryHandler),
        typeof(FSH.Modules.Auditing.Contracts.AuditEnvelope),
        typeof(FSH.Modules.Auditing.Persistence.AuditDbContext)];
});

var moduleAssemblies = new Assembly[]
{
    typeof(IdentityModule).Assembly,
    typeof(MultitenancyModule).Assembly,
    typeof(AuditingModule).Assembly,
    typeof(MessagingModule).Assembly
};

builder.AddHeroPlatform(o =>
{
    o.EnableCaching = true;
    o.EnableMailing = true;
    o.EnableJobs = true;
});

builder.AddModules(moduleAssemblies);
var app = builder.Build();

app.UseHeroMultiTenantDatabases();
app.UseHeroPlatform(p =>
{
    p.MapModules = true;
    p.ServeStaticFiles = true;
});

app.MapGet("/", () => Results.Ok(new { message = "hello world!" }))
   .WithTags("PlayGround")
   .AllowAnonymous();

// Map External Gateway endpoints
app.MapExternalGatewayInvoiceEndpoints();
app.MapDynamicSqlQueryEndpoints();

await app.RunAsync();
