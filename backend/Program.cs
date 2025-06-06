using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Services;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.SignalR;
using backend.Hubs;
using backend;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MySql.Data.MySqlClient;
using Microsoft.EntityFrameworkCore.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Настройка логирования
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins(
                "http://localhost:3000",
                "https://alekseybook.netlify.app"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.MaximumReceiveMessageSize = 102400; // 100 KB
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
    options.KeepAliveInterval = TimeSpan.FromSeconds(10);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT:Key не настроен"))
        )
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            
            if (!string.IsNullOrEmpty(accessToken) && 
                (path.StartsWithSegments(Config.ChatHubUrl.Replace(Config.BackendUrl, "")) || 
                 path.StartsWithSegments(Config.OnlineStatusHubUrl.Replace(Config.BackendUrl, "")) ||
                 path.StartsWithSegments("/hubs/notification")))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// Получаем параметры подключения из переменных окружения
var mysqlHost = Environment.GetEnvironmentVariable("RAILWAY_TCP_PROXY_DOMAIN") ?? "localhost";
var mysqlPort = Environment.GetEnvironmentVariable("RAILWAY_TCP_PROXY_PORT") ?? "3306";
var mysqlDatabase = Environment.GetEnvironmentVariable("MYSQL_DATABASE") ?? "railway";
var mysqlUser = Environment.GetEnvironmentVariable("MYSQLUSER") ?? "root";
var mysqlPassword = Environment.GetEnvironmentVariable("MYSQL_ROOT_PASSWORD") ?? "VWnQlbCWEFNuXEVuNTCZFTgkAPfDCRww";

logger.LogInformation($"Database config: Host={mysqlHost}, Port={mysqlPort}, Database={mysqlDatabase}, User={mysqlUser}");

string connectionString;
string maskedConnectionString;

try
{
    var mySqlOptions = new MySqlConnectionStringBuilder
    {
        Server = mysqlHost,
        Port = uint.Parse(mysqlPort),
        Database = mysqlDatabase,
        UserID = mysqlUser,
        Password = mysqlPassword,
        AllowUserVariables = true,
        AllowPublicKeyRetrieval = true,
        SslMode = MySqlSslMode.VerifyCA,
        ConnectionTimeout = 180,
        DefaultCommandTimeout = 180,
        MaximumPoolSize = 100,
        MinimumPoolSize = 10,
        Pooling = true,
        TreatTinyAsBoolean = true,
        UseCompression = true,
        AutoEnlist = true,
        ConnectionLifeTime = 300,
        ConnectionReset = true,
        PersistSecurityInfo = false,
        AllowLoadLocalInfile = false,
        UseAffectedRows = true
    };

    connectionString = mySqlOptions.ConnectionString;
    maskedConnectionString = connectionString.Replace(mysqlPassword, "*****");
    
    logger.LogInformation($"Connection string being used: {maskedConnectionString}");
    
    // Тестируем подключение
    logger.LogInformation("Testing connection with MySqlConnectionStringBuilder...");
    using (var connection = new MySqlConnection(connectionString))
    {
        try
        {
            await connection.OpenAsync();
            logger.LogInformation("Test connection successful!");
            await connection.CloseAsync();
        }
        catch (Exception ex)
        {
            logger.LogError($"Test connection failed: {ex.Message}");
            logger.LogError($"Inner exception: {ex.InnerException?.Message ?? "No inner exception"}");
            throw;
        }
    }
}
catch (Exception ex)
{
    logger.LogError($"Error configuring connection string: {ex.Message}");
    logger.LogError($"Inner exception: {ex.InnerException?.Message ?? "No inner exception"}");
    throw;
}

// Добавляем DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    try 
    {
        var serverVersion = new MySqlServerVersion(new Version(8, 0, 0));
        logger.LogInformation($"Using MySQL server version: {serverVersion.ToString()}");

        options.UseMySql(
            connectionString,
            serverVersion,
            mysqlOptions =>
            {
                mysqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 10,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null
                );
                mysqlOptions.CommandTimeout((int)TimeSpan.FromMinutes(3).TotalSeconds);
                mysqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            }
        );
        logger.LogInformation("Successfully configured database connection");
    }
    catch (Exception ex)
    {
        logger.LogError($"Error configuring database: {ex.Message}");
        logger.LogError($"Inner exception: {ex.InnerException?.Message ?? "No inner exception"}");
        throw;
    }
});

// Добавляем health checks с более подробной диагностикой
builder.Services.AddHealthChecks()
    .AddMySql(
        connectionString,
        name: "database",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "db", "mysql", "ready" },
        timeout: TimeSpan.FromSeconds(30)
    );

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IFriendService, FriendService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IWallPostService, WallPostService>();
builder.Services.AddScoped<ILikeCommentService, LikeCommentService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Проверяем подключение к базе данных
try
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    logger.LogInformation("Attempting to connect to database...");
    await dbContext.Database.OpenConnectionAsync();
    logger.LogInformation("Database connection successful");
    await dbContext.Database.CloseConnectionAsync();
    
    // Применяем миграции
    logger.LogInformation("Attempting to apply migrations...");
    await dbContext.Database.MigrateAsync();
    logger.LogInformation("Database migrations applied successfully");
}
catch (Exception ex)
{
    logger.LogError($"Database connection error: {ex.Message}");
    logger.LogError($"Inner exception: {ex.InnerException?.Message ?? "No inner exception"}");
    logger.LogError($"Connection string used (masked): {maskedConnectionString}");
    throw;
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

var wwwrootPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
if (!Directory.Exists(wwwrootPath))
{
    Directory.CreateDirectory(wwwrootPath);
}

app.UseStaticFiles();

var uploadsPath = Path.Combine(wwwrootPath, "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>(Config.ChatHubUrl.Replace(Config.BackendUrl, ""));
app.MapHub<OnlineStatusHub>(Config.OnlineStatusHubUrl.Replace(Config.BackendUrl, ""));
app.MapHub<NotificationHub>("/hubs/notification");

// Добавляем endpoint для health check
app.MapHealthChecks("/api/auth/health");

app.Run();


