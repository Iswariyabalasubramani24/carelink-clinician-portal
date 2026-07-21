using Azure.Monitor.OpenTelemetry.AspNetCore;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// OpenTelemetry -> Azure Monitor (Application Insights). Enabled only when a
// connection string is configured; cloud role name comes from OTEL_SERVICE_NAME.
if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    builder.Services.AddOpenTelemetry().UseAzureMonitor();
}

// The environment-specific file fully replaces the route table when present:
// ocelot.json targets localhost:5010 for local dev, while
// ocelot.Production.json targets the "api" container/service hostname.
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile($"ocelot.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

builder.Services.AddOpenApi();
builder.Services.AddOcelot(builder.Configuration);

// In production the SPA is served from the same origin (nginx proxies /api to
// this gateway), so no cross-origin caller exists; the localhost default only
// matters for local `ng serve` development.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:4200"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowFrontend");

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/health")
    {
        await Results.Ok(new { status = "healthy", service = "api-gateway" }).ExecuteAsync(context);
        return;
    }
    await next();
});

await app.UseOcelot();

app.Run();
