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
        builder.WithOrigins(Config.FrontendUrl)
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

// Получаем строку подключения
var mysqlUrl = Environment.GetEnvironmentVariable("MYSQL_URL") ?? "mysql://root:VWnQlbCWEFNuXEVuNTCZFTgkAPfDCRww@mysql-xje1.railway.internal:3306/railway";
logger.LogInformation($"MYSQL_URL: {mysqlUrl}");

string connectionString;
try
{
    var uri = new Uri(mysqlUrl);
    var userInfo = uri.UserInfo.Split(':');
    var host = uri.Host;
    var port = uri.Port;
    var database = uri.AbsolutePath.TrimStart('/');
    var user = userInfo[0];
    var password = userInfo[1];

    connectionString = $"Server={host};Port={port};Database={database};User={user};Password={password};AllowPublicKeyRetrieval=true;SslMode=Required;ConnectionTimeout=180;DefaultCommandTimeout=180;MaximumPoolSize=100;MinimumPoolSize=10;";
    logger.LogInformation($"Parsed connection info: Server={host};Port={port};Database={database};User={user}");
}
catch (Exception ex)
{
    logger.LogError($"Error parsing MYSQL_URL: {ex.Message}");
    throw;
}

// Добавляем DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    try 
    {
        options.UseMySql(
            connectionString,
            ServerVersion.AutoDetect(connectionString),
            mysqlOptions =>
            {
                mysqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 10,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null
                );
            }
        );
        logger.LogInformation("Successfully configured database connection");
    }
    catch (Exception ex)
    {
        logger.LogError($"Error configuring database: {ex.Message}");
        throw;
    }
});

// Добавляем health checks
builder.Services.AddHealthChecks()
    .AddMySql(connectionString, name: "database", failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy);

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
    await dbContext.Database.MigrateAsync();
    logger.LogInformation("Database migrations applied successfully");
}
catch (Exception ex)
{
    logger.LogError($"Database connection error: {ex.Message}");
    logger.LogError($"Connection string used (masked): {connectionString.Replace(connectionString.Split(';').FirstOrDefault(x => x.StartsWith("Password=")) ?? "", "Password=*****")}");
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


