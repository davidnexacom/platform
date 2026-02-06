var builder = DistributedApplication.CreateBuilder(args);

// Postgres container + database
var postgres = builder
    .AddPostgres("postgres")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithContainerName("postgres-fsh-playground")
    .WithDataVolume("fsh-postgres-data")
    .AddDatabase("fsh")
   ;

// SQL Server container for testing External Gateway
var sqlserver = builder
    .AddSqlServer("sqlserver")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithContainerName("sqlserver-fsh-playground")
    .WithDataVolume("fsh-sqlserver-data")
    .AddDatabase("ClientDB");

var redis = builder
    .AddRedis("redis")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithContainerName("redis-fsh-playground")
    .WithDataVolume("fsh-redis-data");

// RabbitMQ container with credentials from parameters (set in appsettings.Development.json)
var rabbitmqUsername = builder.AddParameter("rabbitmq-username", secret: false);
var rabbitmqPassword = builder.AddParameter("rabbitmq-password", secret: true);

var rabbitmq = builder
    .AddRabbitMQ("rabbitmq", userName: rabbitmqUsername, password: rabbitmqPassword)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithContainerName("rabbitmq-fsh-playground")
    .WithDataVolume("fsh-rabbitmq-data")
    .WithManagementPlugin();

builder.AddProject<Projects.Playground_Api>("playground-api")
    .WithReference(postgres)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("OpenTelemetryOptions__Exporter__Otlp__Endpoint", "https://localhost:4317")
    .WithEnvironment("OpenTelemetryOptions__Exporter__Otlp__Protocol", "grpc")
    .WithEnvironment("OpenTelemetryOptions__Exporter__Otlp__Enabled", "true")
    .WithEnvironment("DatabaseOptions__Provider", "POSTGRESQL")
    .WithEnvironment("DatabaseOptions__MigrationsAssembly", "FSH.Playground.Migrations.PostgreSQL")
    .WaitFor(postgres)
    .WaitFor(redis)
    .WaitFor(rabbitmq);

builder.AddProject<Projects.Playground_Blazor>("playground-blazor");

// External Gateway - Cliente de prueba local
builder.AddProject<Projects.FSH_ExternalGateway_Host>("external-gateway")
    .WithReference(sqlserver)
    .WithReference(rabbitmq)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("GatewayOptions__ClientId", "local-test")
    .WithEnvironment("GatewayOptions__SchemaVersion", "v1")
    .WithEnvironment("GatewayOptions__DefaultTimeoutSeconds", "30")
    .WithEnvironment("GatewayOptions__EnableQueryLogging", "true")
    .WaitFor(sqlserver)
    .WaitFor(rabbitmq);

await builder.Build().RunAsync();
