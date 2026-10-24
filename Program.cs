using System.Reflection;
using System.Text.Json.Serialization;
using HospitalBillingApi.Data;
using HospitalBillingApi.Middleware;
using HospitalBillingApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Database Configuration (SQLite Code-First)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=hospital_billing.db";
builder.Services.AddDbContext<HospitalDbContext>(options =>
    options.UseSqlite(connectionString, sqliteOptions =>
        sqliteOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

// 2. Domain & Helper Services
builder.Services.AddScoped<IIdentifierGenerator, IdentifierGenerator>();

// 3. Controllers & JSON Serialization Options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// 4. CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 5. Swagger / OpenAPI Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Hospital Billing & Fee Management System API",
        Version = "v1",
        Description = "Production-ready REST API for OPD & IPD patient billing, bed occupancy tracking, doctor consultations, and revenue settlement calculations.",
        Contact = new OpenApiContact
        {
            Name = "Hospital Billing Administration",
            Email = "admin@hospitalbilling.local"
        }
    });

    // Include XML comments if present
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// 6. Seed Database on Startup
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
        await DbInitializer.SeedAsync(dbContext);
        logger.LogInformation("Database initialized and verified successfully with seed data.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// 7. HTTP Request Pipeline
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseCors("AllowAll");

// Enable Swagger UI at both root / and /swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hospital Billing API v1");
    c.RoutePrefix = string.Empty; // Serves Swagger UI at application root URL
});

app.UseAuthorization();

app.MapControllers();

app.Run();
