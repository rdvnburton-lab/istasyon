using IstasyonDemo.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Npgsql;

namespace IstasyonDemo.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "admin")]
    public class MaintenanceController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly string _backupFolder;

        public MaintenanceController(IWebHostEnvironment env, IConfiguration config)
        {
            _env = env;
            _config = config;
            _backupFolder = Path.Combine(_env.ContentRootPath, "Backups");
            if (!Directory.Exists(_backupFolder)) Directory.CreateDirectory(_backupFolder);
        }

        [HttpGet("backups")]
        public IActionResult GetBackups()
        {
            var files = Directory.GetFiles(_backupFolder, "*.sql")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .Select(f => new BackupFileDto
                {
                    FileName = f.Name,
                    SizeBytes = f.Length,
                    SizePretty = $"{(f.Length / 1024.0 / 1024.0):F2} MB",
                    CreatedAt = f.CreationTime
                })
                .ToList();

            return Ok(files);
        }

        [HttpPost("backup")]
        public async Task<IActionResult> CreateBackup()
        {
            try
            {
                var connString = _config.GetConnectionString("DefaultConnection");
                var builder = new Npgsql.NpgsqlConnectionStringBuilder(connString);
                
                var db = builder.Database;
                var user = builder.Username;
                var pass = builder.Password;

                var fileName = $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.sql";
                var filePath = Path.Combine(_backupFolder, fileName);
                
                // Step 1: Run pg_dump directly using installed client
                var dumpStartInfo = new ProcessStartInfo
                {
                    FileName = "pg_dump",
                    Arguments = $"-h db -U {user} -d {db} -f \"{filePath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // Pass password via environment variable for security
                dumpStartInfo.EnvironmentVariables["PGPASSWORD"] = pass;

                using var dumpProcess = Process.Start(dumpStartInfo);
                if (dumpProcess == null) return StatusCode(500, "Failed to start pg_dump process.");

                var dumpError = await dumpProcess.StandardError.ReadToEndAsync();
                await dumpProcess.WaitForExitAsync();

                if (dumpProcess.ExitCode != 0)
                {
                    return StatusCode(500, $"Backup failed: {dumpError}");
                }

                return Ok(new { message = "Backup created successfully", fileName });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal error: {ex.Message}");
            }
            finally
            {
                // Her halükarda temizlik yap
                CleanupOldBackups();
            }
        }

        private void CleanupOldBackups()
        {
            try
            {
                var retentionDays = 7; // Son 7 günün yedeğini tut
                var files = Directory.GetFiles(_backupFolder, "*.sql")
                                     .Select(f => new FileInfo(f))
                                     .Where(f => f.CreationTime < DateTime.Now.AddDays(-retentionDays))
                                     .ToList();

                foreach (var file in files)
                {
                    try 
                    { 
                        file.Delete(); 
                        Console.WriteLine($"Eski yedek silindi: {file.Name}");
                    } 
                    catch { /* Silinemezse logla ama akışı bozma */ }
                }
            }
            catch { /* Genel hata yutulur */ }
        }

        [HttpGet("backups/download/{fileName}")]
        public IActionResult DownloadBackup(string fileName)
        {
            var filePath = Path.Combine(_backupFolder, fileName);
            if (!System.IO.File.Exists(filePath)) return NotFound();

            var bytes = System.IO.File.ReadAllBytes(filePath);
            return File(bytes, "application/sql", fileName);
        }

        [HttpDelete("backups/{fileName}")]
        public IActionResult DeleteBackup(string fileName)
        {
            var filePath = Path.Combine(_backupFolder, fileName);
            if (!System.IO.File.Exists(filePath)) return NotFound();

            System.IO.File.Delete(filePath);
            return Ok();
        }

        [HttpGet("logs")]
        public IActionResult GetLogs([FromQuery] LogQueryDto query)
        {
            try
            {
                var logDir = Path.Combine(_env.ContentRootPath, "Logs");
                if (!Directory.Exists(logDir)) return Ok(new { logs = new List<LogEntryDto>(), total = 0 });

                var logFiles = Directory.GetFiles(logDir, "log-*.txt")
                    .OrderByDescending(f => f)
                    .ToList();

                var allLogs = new List<LogEntryDto>();

                foreach (var file in logFiles)
                {
                    // Use FileStream with FileShare.ReadWrite to avoid locking issues with Serilog
                    using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var sr = new StreamReader(fs);
                    
                    var fileLines = new List<string>();
                    string? line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        fileLines.Add(line);
                    }
                    
                    fileLines.Reverse(); // Newest first in this file

                    foreach (var l in fileLines)
                    {
                        // Parse line: 2025-12-25 11:13:10.954 +03:00 [INF] Message
                        var logEntry = ParseLogLine(l);
                        if (logEntry == null) continue;

                        // Apply filters
                        if (!string.IsNullOrEmpty(query.Level) && !logEntry.Level.Contains(query.Level, StringComparison.OrdinalIgnoreCase)) continue;
                        if (!string.IsNullOrEmpty(query.SearchTerm) && !logEntry.Message.Contains(query.SearchTerm, StringComparison.OrdinalIgnoreCase)) continue;
                        if (query.StartDate.HasValue && logEntry.Timestamp < query.StartDate.Value) continue;
                        if (query.EndDate.HasValue && logEntry.Timestamp > query.EndDate.Value) continue;

                        allLogs.Add(logEntry);
                        if (allLogs.Count >= 1000) break; // Limit total search
                    }
                    if (allLogs.Count >= 1000) break;
                }

                var total = allLogs.Count;
                var pagedLogs = allLogs
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToList();

                return Ok(new { logs = pagedLogs, total });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to read logs: {ex.Message}");
            }
        }

        private LogEntryDto? ParseLogLine(string line)
        {
            try
            {
                // Format: 2025-12-25 11:13:10.954 +03:00 [INF] Message
                var parts = line.Split(' ', 5);
                if (parts.Length < 4) return null;

                if (!DateTime.TryParse(parts[0] + " " + parts[1], out var timestamp)) return null;

                var levelPart = parts[3]; // [INF], [ERR], etc.
                var level = levelPart.Trim('[', ']');
                var message = parts.Length > 4 ? parts[4] : "";

                return new LogEntryDto
                {
                    Timestamp = timestamp,
                    Level = level,
                    Message = message
                };
            }
            catch { return null; }
        }
    }
}
