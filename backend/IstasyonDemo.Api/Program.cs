using IstasyonDemo.Api.Data;
using IstasyonDemo.Api.Models;
using Microsoft.EntityFrameworkCore;
using IstasyonDemo.Api.Middleware;
using IstasyonDemo.Api.Services;
using FluentValidation;
using FluentValidation.AspNetCore;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;

using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddHttpClient<IstasyonDemo.Api.Services.GeminiService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IVardiyaService, VardiyaService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IFcmService, FcmService>();

// Firebase Admin SDK Başlatma
try
{
    // Google Credentials dosyasının yolu veya içeriği environment variable'dan alınabilir
    string credentialPath = builder.Configuration["Firebase:CredentialPath"] ?? "firebase-adminsdk.json";
    string credentialContent = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIAL_JSON");
    
    if (!string.IsNullOrEmpty(credentialContent))
    {
         FirebaseAdmin.FirebaseApp.Create(new FirebaseAdmin.AppOptions()
        {
            Credential = Google.Apis.Auth.OAuth2.GoogleCredential.FromJson(credentialContent)
        });
        Log.Information("Firebase Admin SDK ortam değişkeninden başarıyla başlatıldı.");
    }
    else if (File.Exists(credentialPath))
    {
        FirebaseAdmin.FirebaseApp.Create(new FirebaseAdmin.AppOptions()
        {
            Credential = Google.Apis.Auth.OAuth2.GoogleCredential.FromFile(credentialPath)
        });
        Log.Information("Firebase Admin SDK başarıyla başlatıldı.");
    }
    else
    {
        Log.Warning($"Firebase credential dosyası bulunamadı: {credentialPath}. Push bildirimleri çalışmayacak.");
    }
}
catch (Exception ex)
{
    Log.Error(ex, "Firebase Admin SDK başlatılırken hata oluştu.");
}

// Proxy arkasında gerçek IP'yi görmek için gerekli (Rate Limiting için kritik)
builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | 
                             Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear(); // Docker ağları için güvenliği esnetiyoruz
    options.KnownProxies.Clear();
});

// Rate Limiting (Hız Sınırlama)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    // IP Bazlı Sınırlama: Dakikada 300 istek
    options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: remoteIp,
            factory: partition => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 300,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    // Auth Endpointleri için daha sıkı limit (Dakikada 10 istek)
    options.AddPolicy("AuthPolicy", context =>
    {
        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: remoteIp,
            factory: partition => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });
});
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() 
                         ?? new[] { 
                             "http://localhost:4200", 
                             "https://istasyon.tiginteknoloji.tr",
                             "http://istasyon.tiginteknoloji.tr" 
                         };

    options.AddPolicy("AllowAngular",
        policy =>
        {
            policy.WithOrigins(allowedOrigins)
                   .AllowAnyMethod()
                   .AllowAnyHeader()
                   .AllowCredentials();
        });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));



// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "super_secret_key_change_this_in_production_12345";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });


// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddHealthChecks();

var app = builder.Build();

// Uygulama başlarken veritabanını otomatik güncelle (Migration)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        // Wait for DB to be ready and apply migrations
        int retryCount = 0;
        bool connected = false;
        while (retryCount < 10 && !connected)
        {
            try
            {
                logger.LogInformation("Veritabanına bağlanılıyor (Deneme {RetryCount})...", retryCount + 1);
                context.Database.Migrate();
                connected = true;
                logger.LogInformation("Veritabanı migration işlemleri başarıyla tamamlandı.");
            }
            catch (Exception ex)
            {
                retryCount++;
                logger.LogWarning("Veritabanı henüz hazır değil veya migration hatası: {Message}. 5 saniye sonra tekrar denenecek...", ex.Message);
                Thread.Sleep(5000);
                if (retryCount >= 10)
                {
                    logger.LogCritical(ex, "Veritabanı migration işlemi 10 deneme sonunda başarısız oldu. Uygulama durduruluyor.");
                    throw; // Rethrow to stop the app if migrations fail
                }
            }
        }

        // 1. Seed Roles
        if (!context.Roles.Any())
        {
            context.Roles.AddRange(
                new Role { Ad = "admin", Aciklama = "Sistem Yöneticisi", IsSystemRole = true },
                new Role { Ad = "patron", Aciklama = "İstasyon Sahibi", IsSystemRole = true },
                new Role { Ad = "istasyon sorumlusu", Aciklama = "İstasyon Sorumlusu", IsSystemRole = true },
                new Role { Ad = "vardiya sorumlusu", Aciklama = "Vardiya Sorumlusu", IsSystemRole = true },
                new Role { Ad = "market sorumlusu", Aciklama = "Market Sorumlusu", IsSystemRole = true }
            );
            context.SaveChanges();
        }

        var adminRole = context.Roles.First(r => r.Ad == "admin");

        // 2. Seed Users
        // Admin
        if (!context.Users.Any(u => u.Username == "admin"))
        {
            context.Users.Add(new User { Username = "admin", RoleId = adminRole.Id, PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123") });
            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogCritical(ex, "Veritabanı başlatma işlemi sırasında kritik bir hata oluştu.");
        throw;
    }
}



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseRouting();

// CORS policy must be between UseRouting and UseEndpoints/MapControllers
// CORS policy must be between UseRouting and UseEndpoints/MapControllers
app.UseForwardedHeaders(); // En üstte olmalı (IP tespiti için)
app.UseCors("AllowAngular");
app.UseRateLimiter(); // CORS'tan sonra çalışmalı

// app.UseHttpsRedirection(); // Coolify/Traefik zaten HTTPS yönetiyor, uygulama içinde yönlendirme çakışma yapabilir


app.UseAuthentication();
app.UseMiddleware<UserActivityMiddleware>();
app.UseAuthorization();

app.MapControllers();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapHealthChecks("/health");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
