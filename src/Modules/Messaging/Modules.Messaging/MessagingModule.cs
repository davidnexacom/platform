using Asp.Versioning;
using FSH.Framework.Web.Modules;
using FSH.Modules.Messaging.Configuration;
using FSH.Modules.Messaging.Contracts.Services;
using FSH.Modules.Messaging.HealthChecks;
using FSH.Modules.Messaging.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Logging;
using Rebus.Routing.TypeBased;
using Rebus.Transport;

namespace FSH.Modules.Messaging;

/// <summary>
/// Messaging module for RabbitMQ integration using Rebus.
/// </summary>
public sealed class MessagingModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var configuration = builder.Configuration;
        var services = builder.Services;

        // Bind configuration
        var rabbitMqOptions = configuration
            .GetSection(RabbitMqOptions.SectionName)
            .Get<RabbitMqOptions>() ?? new RabbitMqOptions();

        services.Configure<RabbitMqOptions>(
            configuration.GetSection(RabbitMqOptions.SectionName));

        if (!rabbitMqOptions.Enabled)
        {
            var serviceProvider = builder.Services.BuildServiceProvider();
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            if (loggerFactory != null)
            {
                var logger = loggerFactory.CreateLogger<MessagingModule>();
                logger.LogInformation("Messaging module is disabled");
            }
            serviceProvider?.Dispose();
            return;
        }

        // Configure Rebus manually without ServiceProvider extension
        services.AddSingleton(provider =>
        {
            var activator = new DependencyInjectionHandlerActivator(provider);
            
            var bus = Configure.With(activator)
                .Logging(l => l.Console())
                .Transport(t => t.UseRabbitMq(rabbitMqOptions.ConnectionString, rabbitMqOptions.QueueName))
                .Routing(r => r.TypeBased())
                .Options(o =>
                {
                    o.SetMaxParallelism(rabbitMqOptions.MaxConcurrentMessages);
                    o.SetNumberOfWorkers(rabbitMqOptions.MaxConcurrentMessages);
                })
                .Start();
            
            return bus;
        });

        // Register message bus service
        services.AddScoped<IMessageBus, RebusMessageBus>();

        // Add health check
        services.AddHealthChecks()
            .AddCheck<RabbitMqHealthCheck>(
                name: "rabbitmq",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["messaging", "rabbitmq"]);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var apiVersionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var group = endpoints
            .MapGroup("api/v{version:apiVersion}/messaging")
            .WithTags("Messaging")
            .WithApiVersionSet(apiVersionSet);

        // Health check endpoint
        group.MapGet("/health", () =>
        {
            return Results.Ok(new
            {
                Status = "Healthy",
                Module = MessagingModuleConstants.ModuleName,
                Timestamp = DateTimeOffset.UtcNow
            });
        })
        .WithName("GetMessagingHealth")
        .WithSummary("Get messaging module health status")
        .Produces(StatusCodes.Status200OK)
        .AllowAnonymous();
    }
}

/// <summary>
/// Custom handler activator that uses dependency injection to resolve handlers.
/// </summary>
internal sealed class DependencyInjectionHandlerActivator : IHandlerActivator
{
    private readonly IServiceProvider _serviceProvider;

    public DependencyInjectionHandlerActivator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public async Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(
        TMessage message,
        ITransactionContext transactionContext)
    {
        var scope = _serviceProvider.CreateScope();
        
        // Dispose scope when transaction is disposed
        transactionContext.OnDisposed(_ =>
        {
            scope?.Dispose();
        });
        
        var handlers = scope.ServiceProvider.GetServices<IHandleMessages<TMessage>>();
        return await Task.FromResult(handlers);
    }
}
