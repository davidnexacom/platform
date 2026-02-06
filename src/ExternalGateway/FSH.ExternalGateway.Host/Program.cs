using FSH.ExternalGateway;
using FSH.ExternalGateway.Configuration;
using FSH.ExternalGateway.Handlers;
using FSH.ExternalGateway.Messages;
using FSH.ExternalGateway.Messages.v1;
using FSH.ExternalGateway.Services;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.ServiceProvider;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/gateway-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog();
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "FSH External Gateway";
    });

    // Get SQL Server connection string from Aspire or configuration
    var sqlConnectionString = builder.Configuration.GetConnectionString("ClientDB");
    var rabbitMqConnectionString = builder.Configuration.GetConnectionString("rabbitmq");

    // Configuration - Override ConnectionString from Aspire if available
    builder.Services.Configure<GatewayOptions>(options =>
    {
        builder.Configuration.GetSection(nameof(GatewayOptions)).Bind(options);
        
        // Use Aspire SQL Server connection if available, otherwise use config
        if (!string.IsNullOrEmpty(sqlConnectionString))
        {
            options.ConnectionString = sqlConnectionString;
            Log.Information("Using SQL Server connection from Aspire: ClientDB");
        }
        else if (string.IsNullOrEmpty(options.ConnectionString))
        {
            Log.Warning("SQL ConnectionString not configured. Gateway will fail when processing queries.");
        }
    });

    builder.Services.Configure<RabbitMqOptions>(
        builder.Configuration.GetSection(nameof(RabbitMqOptions)));

    // Services
    builder.Services.AddSingleton<IQueryRepository, QueryRepository>();
    builder.Services.AddScoped<ISqlQueryExecutor, SqlQueryExecutor>();
    builder.Services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();

    // Get configuration values
    var gatewayOptions = builder.Configuration
        .GetSection(nameof(GatewayOptions))
        .Get<GatewayOptions>()
        ?? throw new InvalidOperationException("GatewayOptions not configured");

    var rabbitMqOptions = builder.Configuration
        .GetSection(nameof(RabbitMqOptions))
        .Get<RabbitMqOptions>()
        ?? new RabbitMqOptions();

    // Use Aspire RabbitMQ connection if available, otherwise use configured one
    var rabbitConnectionString = !string.IsNullOrEmpty(rabbitMqConnectionString) 
        ? rabbitMqConnectionString 
        : rabbitMqOptions.ConnectionString;

    if (string.IsNullOrEmpty(rabbitConnectionString))
    {
        throw new InvalidOperationException("RabbitMQ connection string not configured");
    }

    // Queue name specific to this client
    var queueName = !string.IsNullOrEmpty(rabbitMqOptions.QueueName)
        ? rabbitMqOptions.QueueName
        : $"fsh.gateway.{gatewayOptions.ClientId}.queue";

    // Get actual SQL connection (from Aspire or config)
    var actualSqlConnection = !string.IsNullOrEmpty(sqlConnectionString) 
        ? sqlConnectionString 
        : gatewayOptions.ConnectionString;

    var sqlSource = !string.IsNullOrEmpty(sqlConnectionString) ? "Aspire" : "Config";
    var sqlStatus = string.IsNullOrEmpty(actualSqlConnection) ? "NOT CONFIGURED" : $"Configured (Source: {sqlSource})";

    Log.Information("Starting External Gateway for Client: {ClientId}", gatewayOptions.ClientId);
    Log.Information("RabbitMQ Connection: {ConnectionString}", rabbitConnectionString);
    Log.Information("Queue Name: {QueueName}", queueName);
    Log.Information("SQL Server: {SqlConnection}", sqlStatus);

    // Rebus configuration with TypeBased routing
    builder.Services.AddRebus(configure => configure
        .Logging(l => l.Serilog())
        .Transport(t => t.UseRabbitMq(rabbitConnectionString, queueName)
            .ClientConnectionName($"FSH.Gateway-{gatewayOptions.ClientId}")
            .SetPublisherConfirms(enabled: true))
        .Options(o =>
        {
            o.SetNumberOfWorkers(rabbitMqOptions.MaxConcurrentMessages > 0 
                ? rabbitMqOptions.MaxConcurrentMessages 
                : 5);
            o.SetMaxParallelism(rabbitMqOptions.MaxConcurrentMessages > 0 
                ? rabbitMqOptions.MaxConcurrentMessages 
                : 5);
        })
        .Routing(r => r.TypeBased()
            // Map request messages to this gateway's specific queue
            .Map<GetInvoiceRequest>(queueName)),
        onCreated: async bus =>
        {
            Log.Information("Subscribing to GetInvoiceRequest messages on queue: {QueueName}", queueName);
            await bus.Subscribe<GetInvoiceRequest>();
            
            Log.Information("Subscribing to DynamicSqlQueryRequest messages on queue: {QueueName}", queueName);
            await bus.Subscribe<DynamicSqlQueryRequest>();
            
            Log.Information("Successfully subscribed to all messages");
        });

    // Handlers
    builder.Services.AutoRegisterHandlersFromAssemblyOf<GetInvoiceHandler>();

    // Background Worker
    builder.Services.AddHostedService<GatewayWorker>();

    var host = builder.Build();
    
    Log.Information("External Gateway built successfully for Client: {ClientId}", gatewayOptions.ClientId);

    // Initialize database before starting
    Log.Information("Initializing database...");
    using (var scope = host.Services.CreateScope())
    {
        var dbInitializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
        await dbInitializer.InitializeAsync();
    }
    Log.Information("Database initialization complete");

    Log.Information("Starting External Gateway...");
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "External Gateway terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
