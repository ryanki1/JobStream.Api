using Microsoft.EntityFrameworkCore;
using JobStream.Api.Data;
using JobStream.Api.Services;
using JobStream.Api.Middleware;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Ensure PostgreSQL database exists before configuring DbContext (only in Development)
if (builder.Environment.IsDevelopment())
{
    EnsureDatabaseExists(builder.Configuration.GetConnectionString("DefaultConnection")!);
}

// Configure PostgreSQL Database
builder.Services.AddDbContext<JobStreamDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Infrastructure Services
builder.Services.AddScoped<IStorageService, MockStorageService>();
builder.Services.AddScoped<IEmailService, MockEmailService>();

// Register Encryption Service (AES-256 for production, Mock for development)
// To use real encryption, ensure Encryption:Key and Encryption:IV are set in appsettings.json
if (builder.Environment.IsProduction())
{
    builder.Services.AddScoped<IEncryptionService, AesEncryptionService>();
}
else
{
    // For development, you can use either Mock or AES encryption
    // Uncomment the line below to test with real AES encryption in development
    builder.Services.AddScoped<IEncryptionService, AesEncryptionService>();
    // builder.Services.AddScoped<IEncryptionService, MockEncryptionService>();
}

// Register Business Logic Services
builder.Services.AddScoped<ICompanyRegistrationService, CompanyRegistrationService>();

// Configure CORS for Angular frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Add Controllers with JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "JobStream Company Registration API",
        Version = "v1",
        Description = "RESTful API for company registration workflow with multi-step verification",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "JobStream Support",
            Email = "support@jobstream.com"
        }
    });

    // Enable XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Apply database migrations
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<JobStreamDbContext>();
    dbContext.Database.Migrate();
    app.Logger.LogInformation("Database migrations applied successfully");
}

// Configure the HTTP request pipeline.

// Add global error handling middleware (should be first)
app.UseErrorHandling();

// Enable Swagger in all environments for easier testing
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "JobStream API v1");
    options.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

// Enable CORS
app.UseCors("AngularApp");

// Add rate limiting middleware
app.UseRateLimiting();

app.UseAuthorization();

app.MapControllers();

app.Logger.LogInformation("JobStream API is starting...");
app.Logger.LogInformation("Swagger documentation available at: /swagger");
app.Logger.LogInformation("Company Registration API endpoints:");
app.Logger.LogInformation("  POST   /api/company/register/start");
app.Logger.LogInformation("  POST   /api/company/register/verify-email");
app.Logger.LogInformation("  PUT    /api/company/register/{{id}}/company-details");
app.Logger.LogInformation("  POST   /api/company/register/{{id}}/documents");
app.Logger.LogInformation("  POST   /api/company/register/{{id}}/financial-verification");
app.Logger.LogInformation("  GET    /api/company/register/{{id}}/status");
app.Logger.LogInformation("  POST   /api/company/register/{{id}}/submit");

app.Run();

// Helper method to ensure database and user exist
static void EnsureDatabaseExists(string connectionString)
{
    var builder = new NpgsqlConnectionStringBuilder(connectionString);
    var databaseName = builder.Database;
    var username = builder.Username;
    var password = builder.Password;

    // Connect to 'postgres' database using the default postgres user
    var postgresBuilder = new NpgsqlConnectionStringBuilder
    {
        Host = builder.Host,
        Port = builder.Port,
        Database = "postgres",
        Username = Environment.GetEnvironmentVariable("PGUSER") ?? Environment.UserName, // Use current OS user
        // No password for local development (trust authentication)
    };

    try
    {
        using var connection = new NpgsqlConnection(postgresBuilder.ToString());
        connection.Open();

        // Check if user exists
        using var checkUserCmd = new NpgsqlCommand($"SELECT 1 FROM pg_roles WHERE rolname = '{username}'", connection);
        var userExists = checkUserCmd.ExecuteScalar() != null;

        if (!userExists)
        {
            Console.WriteLine($"User '{username}' does not exist. Creating...");

            // Create the user
            using var createUserCmd = new NpgsqlCommand($"CREATE USER {username} WITH PASSWORD '{password}'", connection);
            createUserCmd.ExecuteNonQuery();

            Console.WriteLine($"User '{username}' created successfully.");
        }
        else
        {
            Console.WriteLine($"User '{username}' already exists.");
        }

        // Check if database exists
        using var checkDbCmd = new NpgsqlCommand($"SELECT 1 FROM pg_database WHERE datname = '{databaseName}'", connection);
        var dbExists = checkDbCmd.ExecuteScalar() != null;

        if (!dbExists)
        {
            Console.WriteLine($"Database '{databaseName}' does not exist. Creating...");

            // Create the database
            using var createDbCmd = new NpgsqlCommand($"CREATE DATABASE {databaseName} OWNER {username}", connection);
            createDbCmd.ExecuteNonQuery();

            Console.WriteLine($"Database '{databaseName}' created successfully.");
        }
        else
        {
            Console.WriteLine($"Database '{databaseName}' already exists.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error ensuring database exists: {ex.Message}");
        Console.WriteLine($"Note: Make sure PostgreSQL is running and your current user has access.");
        Console.WriteLine($"You can grant access with: CREATE USER {Environment.UserName} SUPERUSER;");
        throw;
    }
}
