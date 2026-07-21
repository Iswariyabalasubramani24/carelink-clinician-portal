using System.Text;
using System.Text.Json.Serialization;
using Asp.Versioning;
using CareLink.API.Middleware;
using CareLink.Application.Alerts;
using CareLink.Application.Auth.Commands;
using CareLink.Application.Common.Interfaces;
using CareLink.Infrastructure;
using CareLink.Infrastructure.Auditing;
using CareLink.Infrastructure.Repositories;
using CareLink.Infrastructure.Reports;
using CareLink.Infrastructure.Security;
using CareLink.Infrastructure.Seeding;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "CareLink:";
});

builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "sqlserver", tags: ["ready"])
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!, name: "redis", tags: ["ready"]);

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(LoginCommand).Assembly));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IClinicianRepository, ClinicianRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<ITenantRepository, TenantRepository>();
builder.Services.AddScoped<IClinicianTenantRepository, ClinicianTenantRepository>();
builder.Services.AddScoped<IAlertRepository, AlertRepository>();
builder.Services.AddScoped<IClinicAlertSettingsRepository, ClinicAlertSettingsRepository>();
builder.Services.AddScoped<IPatientAlertSettingsRepository, PatientAlertSettingsRepository>();
builder.Services.AddScoped<IAlertEvaluationService, AlertEvaluationService>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IReportSettingsRepository, ReportSettingsRepository>();
builder.Services.AddScoped<IScheduleSettingsRepository, ScheduleSettingsRepository>();
builder.Services.AddScoped<IReportPdfGenerator, QuestPdfReportGenerator>();
builder.Services.AddScoped<IPatientNoteRepository, PatientNoteRepository>();
builder.Services.AddScoped<IAuditLogger, AuditLogger>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<ITemporaryPasswordGenerator, TemporaryPasswordGenerator>();
builder.Services.AddScoped<SuperAdminSeeder>();
builder.Services.AddScoped<DevelopmentDataSeeder>();
builder.Services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Bootstrap the platform operator (all environments, idempotent) and, in
// development only, the demo hospitals. Schema migrations are applied
// out-of-band (dotnet ef database update / deployment pipeline), so this runs
// against an already-migrated database.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await services.GetRequiredService<SuperAdminSeeder>().SeedAsync(
        builder.Configuration["SuperAdmin:Email"],
        builder.Configuration["SuperAdmin:Password"]);

    if (app.Environment.IsDevelopment())
    {
        await services.GetRequiredService<DevelopmentDataSeeder>().SeedAsync();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "CareLink API v1");
    });
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// Liveness: is the process up and able to handle requests at all? No
// dependency checks - a slow/down DB or Redis must not fail this, or an
// orchestrator would kill and restart a perfectly healthy API instance.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

// Readiness: can this instance actually serve traffic right now? Checks the
// dependencies the API cannot function without.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapControllers();

app.Run();
