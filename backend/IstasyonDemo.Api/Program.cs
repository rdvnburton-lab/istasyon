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
    .WriteTo.Seq(builder.Configuration["Serilog:SeqServerUrl"] ?? "http://localhost:5341")
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IVardiyaService, VardiyaService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
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
                   .AllowCredentials()
                   .SetIsOriginAllowed(origin => true); // Daha esnek eşleşme için
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

        // Seed Default Station
        if (!context.Istasyonlar.Any())
        {
            context.Istasyonlar.Add(new Istasyon { Ad = "Merkez İstasyon", Adres = "Merkez", Aktif = true });
            context.SaveChanges();
        }

        // Seed Roles
        if (!context.Roles.Any())
        {
            context.Roles.AddRange(
                new Role { Ad = "admin", Aciklama = "Sistem Yöneticisi", IsSystemRole = true },
                new Role { Ad = "patron", Aciklama = "İstasyon Sahibi", IsSystemRole = true },
                new Role { Ad = "vardiya sorumlusu", Aciklama = "Vardiya Sorumlusu", IsSystemRole = true },
                new Role { Ad = "market sorumlusu", Aciklama = "Market Sorumlusu", IsSystemRole = true }
            );
            context.SaveChanges();
        }

        var adminRole = context.Roles.First(r => r.Ad == "admin");
        var patronRole = context.Roles.First(r => r.Ad == "patron");
        var vardiyaRole = context.Roles.First(r => r.Ad == "vardiya sorumlusu");

        // Seed Users
        // Admin
        if (!context.Users.Any(u => u.Username == "admin"))
        {
            context.Users.Add(new User { Username = "admin", RoleId = adminRole.Id, PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123") });
            context.SaveChanges();
        }

        // Vardiya Sorumlusu (Demo)
        if (!context.Users.Any(u => u.Username == "vardiya"))
        {
            context.Users.Add(new User { Username = "vardiya", RoleId = vardiyaRole.Id, PasswordHash = BCrypt.Net.BCrypt.HashPassword("vardiya123") });
            context.SaveChanges();
        }

        // Patron (Demo)
        if (!context.Users.Any(u => u.Username == "patron"))
        {
            context.Users.Add(new User { Username = "patron", RoleId = patronRole.Id, PasswordHash = BCrypt.Net.BCrypt.HashPassword("patron123") });
            context.SaveChanges();
        }

        // Ensure Patron owns Merkez Istasyon
        var patronUser = context.Users.FirstOrDefault(u => u.Username == "patron");
        var merkezIstasyon = context.Istasyonlar.FirstOrDefault(u => u.Ad == "Merkez İstasyon");

        if (patronUser != null && merkezIstasyon != null && merkezIstasyon.PatronId == null)
        {
            merkezIstasyon.PatronId = patronUser.Id;
            context.SaveChanges();
        }

        // FIX: Demo kullanıcılarına istasyon ata
        var usersToFix = context.Users.Where(u => u.IstasyonId == null && u.Username != "admin").ToList();
        if (usersToFix.Any() && merkezIstasyon != null)
        {
            foreach (var user in usersToFix)
            {
                user.IstasyonId = merkezIstasyon.Id;
            }
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

app.UseMiddleware<ExceptionMiddleware>();

app.UseRouting();

// CORS policy must be between UseRouting and UseEndpoints/MapControllers
app.UseCors("AllowAngular");

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

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
