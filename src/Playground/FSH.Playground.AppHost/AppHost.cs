var builder = DistributedApplication.CreateBuilder(args);

// Postgres container + database
var postgres = builder
    .AddPostgres("postgres")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithContainerName("postgres-fsh-playground")
    .WithDataVolume("fsh-postgres-data")
    .AddDatabase("fsh")
    ;

var redis = builder
    .AddRedis("redis")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithContainerName("redis-fsh-playground")
    .WithDataVolume("fsh-redis-data");

// RabbitMQ container with management plugin
var rabbitmq = builder
    .AddRabbitMQ("rabbitmq")
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

await builder.Build().RunAsync();
