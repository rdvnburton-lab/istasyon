using IstasyonDemo.Api.Data;
using IstasyonDemo.Api.Dtos;
using IstasyonDemo.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace IstasyonDemo.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "admin")]
    public class SystemHealthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private static readonly DateTime _startTime = DateTime.UtcNow;

        public SystemHealthController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet]
        public async Task<ActionResult<SystemHealthDto>> GetHealth()
        {
            var health = new SystemHealthDto();

            // 1. System Status & Storage
            health.Status = new SystemStatus
            {
                DatabaseStatus = _context.Database.CanConnect() ? "Healthy" : "Unhealthy",
                StorageStatus = "Healthy",
                StorageUsage = "0%",
                AvailableFreeSpace = "0 GB / 0 GB",
                UptimeDays = Math.Round((DateTime.UtcNow - _startTime).TotalDays, 2),
                ServerTime = DateTime.UtcNow.AddHours(3).ToString("yyyy-MM-dd HH:mm:ss") + " (GMT+3)"
            };

            try
            {
                var rootPath = Path.GetPathRoot(_env.ContentRootPath);
                if (!string.IsNullOrEmpty(rootPath))
                {
                    var drive = new DriveInfo(rootPath);
                    if (drive.IsReady)
                    {
                        double freeSpaceGb = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024);
                        double totalSpaceGb = drive.TotalSize / (1024.0 * 1024 * 1024);
                        double usagePercent = 100 - (drive.AvailableFreeSpace * 100.0 / drive.TotalSize);

                        health.Status.StorageStatus = usagePercent > 90 ? "Warning" : "Healthy";
                        health.Status.StorageUsage = $"{usagePercent:F1}%";
                        health.Status.AvailableFreeSpace = $"{freeSpaceGb:F1} GB / {totalSpaceGb:F1} GB";
                    }
                }

                // Directory Writable Checks
                var criticalDirs = new[] { "Logs", "Uploads" };
                foreach (var dir in criticalDirs)
                {
                    var path = Path.Combine(_env.ContentRootPath, dir);
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                    
                    try {
                        var testFile = Path.Combine(path, ".write_test");
                        System.IO.File.WriteAllText(testFile, "test");
                        System.IO.File.Delete(testFile);
                    } catch (Exception ex) {
                        health.Anomalies.Add(new AnomalyDto {
                            Type = "FileSystemError",
                            Severity = "Critical",
                            Description = $"Directory '{dir}' is not writable: {ex.Message}",
                            DetectedAt = DateTime.UtcNow
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                health.Status.StorageStatus = "Error";
                health.Status.StorageUsage = "N/A";
                health.Status.AvailableFreeSpace = "N/A / N/A";
                health.Anomalies.Add(new AnomalyDto {
                    Type = "SystemMetricsError",
                    Severity = "Warning",
                    Description = $"Failed to fetch system metrics: {ex.Message}",
                    DetectedAt = DateTime.UtcNow
                });
            }

            // 2. Database Metrics & Table Stats
            try
            {
                var connection = _context.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open) await connection.OpenAsync();
                
                using (var command = connection.CreateCommand())
                {
                    // DB Size Raw
                    try {
                        command.CommandText = "SELECT pg_database_size(current_database());";
                        var sizeResult = await command.ExecuteScalarAsync();
                        health.Metrics.TotalSizeRaw = sizeResult != null ? Convert.ToInt64(sizeResult) : 0;
                        
                        // DB Size Pretty
                        command.CommandText = $"SELECT pg_size_pretty({health.Metrics.TotalSizeRaw}::bigint);";
                        health.Metrics.TotalSize = (await command.ExecuteScalarAsync())?.ToString() ?? "0 B";
                    } catch (Exception ex) {
                        health.Metrics.TotalSize = "Error";
                        health.Anomalies.Add(new AnomalyDto { Type = "DbSizeError", Severity = "Warning", Description = $"Size check failed: {ex.Message}", DetectedAt = DateTime.UtcNow });
                    }

                    // Active Connections
                    try {
                        command.CommandText = "SELECT count(*) FROM pg_stat_activity WHERE datname = current_database();";
                        health.Metrics.ConnectionCount = Convert.ToInt32(await command.ExecuteScalarAsync());
                    } catch { /* ignore */ }

                    // Table Stats
                    try {
                        command.CommandText = @"
                            SELECT 
                                relname as TableName, 
                                COALESCE(n_live_tup, 0) as RowCount,
                                pg_size_pretty(pg_total_relation_size(relid)) as Size
                            FROM pg_stat_user_tables 
                            WHERE schemaname = 'public'
                            ORDER BY pg_total_relation_size(relid) DESC;";
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                health.TableStats.Add(new TableStatDto
                                {
                                    TableName = reader.IsDBNull(0) ? "Unknown" : reader.GetString(0),
                                    RowCount = reader.IsDBNull(1) ? 0 : reader.GetInt64(1),
                                    Size = reader.IsDBNull(2) ? "0 B" : reader.GetString(2)
                                });
                            }
                        }
                    } catch (Exception ex) {
                        health.Anomalies.Add(new AnomalyDto { Type = "TableStatsError", Severity = "Warning", Description = $"Table stats failed: {ex.Message}", DetectedAt = DateTime.UtcNow });
                    }
                }
                health.Metrics.TotalRows = health.TableStats.Sum(t => t.RowCount);
                
                // Set raw free space from drive info
                var rootPath = Path.GetPathRoot(_env.ContentRootPath);
                if (!string.IsNullOrEmpty(rootPath))
                {
                    var drive = new DriveInfo(rootPath);
                    if (drive.IsReady) {
                        health.Metrics.FreeSpaceRaw = drive.AvailableFreeSpace;
                    }
                }
            }
            catch (Exception ex)
            {
                health.Anomalies.Add(new AnomalyDto
                {
                    Type = "DatabaseError",
                    Severity = "Critical",
                    Description = $"Database connection failed: {ex.Message}",
                    DetectedAt = DateTime.UtcNow
                });
            }

            // 3. Environment Info
            health.Environment = new EnvironmentInfo
            {
                OsVersion = RuntimeInformation.OSDescription,
                RuntimeVersion = RuntimeInformation.FrameworkDescription,
                MachineName = Environment.MachineName,
                ProcessorCount = Environment.ProcessorCount,
                DeploymentMode = _env.IsDevelopment() ? "Development" : "Production"
            };

            // 4. Resource Metrics
            var process = Process.GetCurrentProcess();
            health.Resources = new ResourceMetrics
            {
                MemoryUsageRaw = process.WorkingSet64 / (1024.0 * 1024),
                MemoryUsage = $"{process.WorkingSet64 / (1024.0 * 1024):F1} MB",
                CpuUsageRaw = 0, // Simplified, requires multiple samples for real %
                CpuUsage = "N/A"
            };

            // 5. Security & Active Sessions
            await PopulateSecurityAndSessions(health);

            // 6. Background Tasks (Simulated)
            health.BackgroundTasks = new List<BackgroundTaskDto>
            {
                new BackgroundTaskDto { Name = "Log Cleanup", Status = "Running", LastRun = DateTime.UtcNow.AddHours(-2).ToString("HH:mm"), NextRun = "Tomorrow 00:00" },
                new BackgroundTaskDto { Name = "Data Sync", Status = "Idle", LastRun = DateTime.UtcNow.AddMinutes(-15).ToString("HH:mm"), NextRun = "In 45 min" },
                new BackgroundTaskDto { Name = "Report Generator", Status = "Idle", LastRun = "Yesterday", NextRun = "Monday" }
            };

            // 7. Anomalies (Data Integrity Checks)
            await RunIntegrityChecks(health.Anomalies);

            // 8. Endpoint Health Checks
            await CheckEndpoints(health.Endpoints);

            // 9. Recent Errors from Logs
            health.RecentErrors = GetRecentErrorsFromLogs();

            return Ok(health);
        }

        private async Task PopulateSecurityAndSessions(SystemHealthDto health)
        {
            // Active Sessions (Current users in DB as proxy)
            var users = await _context.Users
                .Include(u => u.Role)
                .OrderByDescending(u => u.Id)
                .Take(10)
                .ToListAsync();
            foreach (var u in users)
            {
                health.ActiveSessions.Add(new ActiveSessionDto
                {
                    Username = u.Username,
                    Role = u.Role?.Ad ?? "N/A",
                    LastActivity = "Recent" // Mock
                });
            }

            // Security Audit (From Logs)
            try
            {
                var logDir = Path.Combine(_env.ContentRootPath, "Logs");
                if (Directory.Exists(logDir))
                {
                    var logFile = Directory.GetFiles(logDir, "log-*.txt")
                        .OrderByDescending(f => f)
                        .FirstOrDefault();

                    if (logFile != null)
                    {
                        using var fs = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        using var sr = new StreamReader(fs);
                        var allFileLines = new List<string>();
                        string? l;
                        while ((l = sr.ReadLine()) != null) allFileLines.Add(l);
                        
                        var lines = allFileLines.AsEnumerable().Reverse().Take(500).ToList();
                        foreach (var line in lines)
                        {
                            if (line.Contains("Login failed") || line.Contains("Unauthorized"))
                            {
                                health.Security.FailedLoginsLast24h++;
                                if (health.Security.RecentEvents.Count < 10)
                                {
                                    health.Security.RecentEvents.Add(new SecurityEventDto
                                    {
                                        Timestamp = DateTime.UtcNow, // Simplified
                                        EventType = "AuthFailure",
                                        Description = line.Length > 100 ? line.Substring(0, 100) + "..." : line,
                                        IpAddress = "Unknown"
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch { /* ignore */ }
        }

        private async Task CheckEndpoints(List<EndpointHealthDto> endpoints)
        {
            var modules = new[]
            {
                new { Name = "Auth Service", Path = "/api/auth/login", Table = "Users" },
                new { Name = "Vardiya Service", Path = "/api/vardiya", Table = "Vardiyalar" },
                new { Name = "User Management", Path = "/api/user", Table = "Users" },
                new { Name = "Station Service", Path = "/api/admin/istasyonlar", Table = "Istasyonlar" },
                new { Name = "Market Service", Path = "/api/marketvardiya", Table = "MarketVardiyalar" }
            };

            foreach (var module in modules)
            {
                var sw = Stopwatch.StartNew();
                var status = "Healthy";
                try
                {
                    var connection = _context.Database.GetDbConnection();
                    if (connection.State != ConnectionState.Open) await connection.OpenAsync();
                    
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = $"SELECT 1 FROM \"{module.Table}\" LIMIT 1";
                        await command.ExecuteScalarAsync();
                    }
                    sw.Stop();
                }
                catch (Exception)
                {
                    status = "Unhealthy";
                    sw.Stop();
                }

                endpoints.Add(new EndpointHealthDto
                {
                    Name = module.Name,
                    Path = module.Path,
                    Status = status,
                    ResponseTimeMs = sw.ElapsedMilliseconds,
                    LastChecked = DateTime.UtcNow.AddHours(3).ToString("HH:mm:ss")
                });
            }
        }

        private async Task RunIntegrityChecks(List<AnomalyDto> anomalies)
        {
            // Check 1: Shifts with very high differences (> 1000 TL)
            var highDiffShifts = await _context.Vardiyalar
                .Where(v => v.Durum != VardiyaDurum.SILINDI)
                .Select(v => new {
                    Vardiya = v,
                    PusulaToplam = _context.Pusulalar.Where(p => p.VardiyaId == v.Id).Sum(p => p.Nakit + p.KrediKarti + (p.DigerOdemeler.Sum(d => d.Tutar))),
                    FiloToplam = _context.FiloSatislar.Where(f => f.VardiyaId == v.Id).Sum(f => f.Tutar)
                })
                .Select(x => new {
                    x.Vardiya,
                    Fark = (x.PusulaToplam + x.FiloToplam) - x.Vardiya.PompaToplam
                })
                .Where(x => Math.Abs(x.Fark) > 1000)
                .OrderByDescending(x => x.Vardiya.BaslangicTarihi)
                .Take(5)
                .ToListAsync();

            foreach (var item in highDiffShifts)
            {
                anomalies.Add(new AnomalyDto
                {
                    Type = "HighDifference",
                    Severity = "Warning",
                    Description = $"Vardiya #{item.Vardiya.Id} has a high difference: {item.Fark:N2} TL",
                    RelatedEntityId = item.Vardiya.Id.ToString(),
                    DetectedAt = DateTime.UtcNow
                });
            }

            // Check 2: Users without Station (except admin)
            var orphanedUsers = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.IstasyonId == null && u.Role != null && u.Role.Ad != "admin")
                .ToListAsync();

            foreach (var u in orphanedUsers)
            {
                anomalies.Add(new AnomalyDto
                {
                    Type = "OrphanedUser",
                    Severity = "Info",
                    Description = $"User '{u.Username}' ({u.Role?.Ad}) is not assigned to any station.",
                    RelatedEntityId = u.Id.ToString(),
                    DetectedAt = DateTime.UtcNow
                });
            }

            // Check 3: Stations without Patron
            var orphanedStations = await _context.Istasyonlar
                .Where(i => i.FirmaId == 0 && i.Aktif)
                .ToListAsync();

            foreach (var i in orphanedStations)
            {
                anomalies.Add(new AnomalyDto
                {
                    Type = "OrphanedStation",
                    Severity = "Warning",
                    Description = $"Station '{i.Ad}' has no owner (Patron).",
                    RelatedEntityId = i.Id.ToString(),
                    DetectedAt = DateTime.UtcNow
                });
            }
        }

        private List<LogEntryDto> GetRecentErrorsFromLogs()
        {
            var errors = new List<LogEntryDto>();
            try
            {
                var logDir = Path.Combine(_env.ContentRootPath, "Logs");
                if (!Directory.Exists(logDir)) return errors;

                var logFile = Directory.GetFiles(logDir, "log-*.txt")
                    .OrderByDescending(f => f)
                    .FirstOrDefault();

                if (logFile == null) return errors;

                // Read last 200 lines to find errors safely
                using var fs = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var sr = new StreamReader(fs);
                var allFileLines = new List<string>();
                string? l;
                while ((l = sr.ReadLine()) != null) allFileLines.Add(l);
                
                var lines = allFileLines.AsEnumerable().Reverse().Take(200).ToList();
                
                foreach (var line in lines)
                {
                    if (line.Contains("[ERR]") || line.Contains("[FTL]"))
                    {
                        // Try to parse timestamp: 2025-12-25 11:13:10.954 +03:00 [ERR]
                        DateTime timestamp = DateTime.UtcNow;
                        var parts = line.Split(' ');
                        if (parts.Length >= 2 && DateTime.TryParse(parts[0] + " " + parts[1], out var parsedDate))
                        {
                            timestamp = parsedDate;
                        }

                        // Only show errors from the last 30 minutes to avoid confusion with fixed issues
                        if (DateTime.UtcNow - timestamp.ToUniversalTime() > TimeSpan.FromMinutes(30))
                            continue;

                        errors.Add(new LogEntryDto
                        {
                            Timestamp = timestamp,
                            Level = line.Contains("[ERR]") ? "Error" : "Fatal",
                            Message = line
                        });
                        if (errors.Count >= 20) break;
                    }
                }
            }
            catch (Exception)
            {
                // Ignore log reading errors
            }
            return errors;
        }
    }
}
