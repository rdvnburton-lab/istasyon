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

// Fix for 'windows-1254' encoding support
System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

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
builder.Services.AddScoped<IVardiyaFinancialService, VardiyaFinancialService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IFcmService, FcmService>();
builder.Services.AddScoped<IMarketVardiyaService, MarketVardiyaService>();
builder.Services.AddScoped<IDefinitionsService, DefinitionsService>();
builder.Services.AddScoped<StokHesaplamaService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IYakitService, YakitService>();
builder.Services.AddScoped<VardiyaArsivService>();

// Firebase Admin SDK Başlatma
try
{
    // Google Credentials dosyasının yolu veya içeriği environment variable'dan alınabilir
    string credentialPath = builder.Configuration["Firebase:CredentialPath"] ?? "firebase-adminsdk.json";
    string? credentialContent = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIAL_JSON");
    
    if (!string.IsNullOrEmpty(credentialContent))
    {
        try 
        {
            // 1. Düz deneme (İdeal durum)
            CreateFirebaseApp(credentialContent);
            Log.Information("Firebase Admin SDK ortam değişkeninden (raw) başarıyla başlatıldı.");
        }
        catch
        {
            Log.Warning("Raw Firebase credential denenirken hata alındı veya format bozuk olabilir. JSON parse edilerek onarılmaya çalışılıyor...");

            try
            {
                // 1. Olası tırnak hatalarını temizle
                string sanitized = credentialContent.Trim();
                if (sanitized.StartsWith("\"") && sanitized.EndsWith("\""))
                {
                    sanitized = sanitized.Substring(1, sanitized.Length - 2);
                }
                // Unescape
                if (sanitized.Contains("\\\""))
                {
                    sanitized = sanitized.Replace("\\\"", "\"");
                }

                // 2. JSON olarak parse et ve private_key'i düzelt
                using (var doc = System.Text.Json.JsonDocument.Parse(sanitized))
                {
                    var root = doc.RootElement.Clone(); // Clone to get a mutable copy check usually needs reconstruction
                    // JsonDocument is readonly. We'll use a dictionary/object approach for modification
                    
                    var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(sanitized);
                    if (dict != null && dict.ContainsKey("private_key"))
                    {
                        string? pk = dict["private_key"]?.ToString();
                        // Literal " \n " stringini gerçek newline karakterine çevir
                        if (pk != null && pk.Contains("\\n"))
                        {
                            pk = pk.Replace("\\n", "\n");
                            dict["private_key"] = pk;
                        }
                        
                        // Tekrar JSON stringe çevir
                        string fixedJson = System.Text.Json.JsonSerializer.Serialize(dict);
                        
                        CreateFirebaseApp(fixedJson);
                        Log.Information("Firebase Admin SDK, onarılmış JSON credential ile başarıyla başlatıldı.");
                    }
                    else
                    {
                        // private_key yoksa belki de onarılması gerekmiyordur veya farklı bir formattır
                        // Yine de sanitized halini deneyelim
                         CreateFirebaseApp(sanitized);
                         Log.Information("Firebase Admin SDK, temizlenmiş (sanitized) credential ile başarıyla başlatıldı.");
                    }
                }
            }
            catch (Exception exFix)
            {
                Log.Error(exFix, "Firebase credential onarma girişimi başarısız oldu.");
                // Son çare log verisi
                var debugContent = credentialContent.Length > 100 ? credentialContent.Substring(0, 100) + "..." : credentialContent;
                 Log.Error($"Başarısız içerik başı: {debugContent}");
            }
        }
    }
    else if (File.Exists(credentialPath))
    {
        FirebaseAdmin.FirebaseApp.Create(new FirebaseAdmin.AppOptions()
        {
            Credential = Google.Apis.Auth.OAuth2.GoogleCredential.FromFile(credentialPath)
        });
        Log.Information("Firebase Admin SDK dosyasından başarıyla başlatıldı.");
    }
    else
    {
        Log.Warning($"Firebase credential dosyası bulunamadı: {credentialPath}. Push bildirimleri çalışmayacak.");
    }
}
catch (Exception ex)
{
    Log.Error(ex, "Firebase Admin SDK genel blokta hata oluştu.");
}

void CreateFirebaseApp(string json) {
    if (FirebaseAdmin.FirebaseApp.DefaultInstance != null) return;
    
    FirebaseAdmin.FirebaseApp.Create(new FirebaseAdmin.AppOptions()
    {
        Credential = Google.Apis.Auth.OAuth2.GoogleCredential.FromJson(json)
    });
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
                new Role { Ad = "market sorumlusu", Aciklama = "Market Sorumlusu", IsSystemRole = true },
                new Role { Ad = "pasif", Aciklama = "Pasif Kullanıcı", IsSystemRole = true }
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
        
        // 3. Seed System Definitions
        var definitionsService = services.GetRequiredService<IDefinitionsService>();
        definitionsService.SeedInitialDataAsync().Wait();
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

// Health Checks
app.MapHealthChecks("/health");

app.Run();
