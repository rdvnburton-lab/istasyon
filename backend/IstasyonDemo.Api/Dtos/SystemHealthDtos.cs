using System;
using System.Collections.Generic;

namespace IstasyonDemo.Api.Dtos
{
    public class SystemHealthDto
    {
        public SystemStatus Status { get; set; } = new();
        public List<AnomalyDto> Anomalies { get; set; } = new();
        public List<LogEntryDto> RecentErrors { get; set; } = new();
        public DatabaseMetrics Metrics { get; set; } = new();
        public List<TableStatDto> TableStats { get; set; } = new();
        public List<EndpointHealthDto> Endpoints { get; set; } = new();
        public EnvironmentInfo Environment { get; set; } = new();
        public ResourceMetrics Resources { get; set; } = new();
        public SecurityAuditDto Security { get; set; } = new();
        public List<ActiveSessionDto> ActiveSessions { get; set; } = new();
        public List<BackgroundTaskDto> BackgroundTasks { get; set; } = new();
    }

    public class EnvironmentInfo
    {
        public string OsVersion { get; set; } = string.Empty;
        public string RuntimeVersion { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public int ProcessorCount { get; set; }
        public string DeploymentMode { get; set; } = string.Empty;
    }

    public class ResourceMetrics
    {
        public string MemoryUsage { get; set; } = string.Empty;
        public double MemoryUsageRaw { get; set; }
        public string CpuUsage { get; set; } = string.Empty;
        public double CpuUsageRaw { get; set; }
    }

    public class SecurityAuditDto
    {
        public int FailedLoginsLast24h { get; set; }
        public List<SecurityEventDto> RecentEvents { get; set; } = new();
    }

    public class SecurityEventDto
    {
        public DateTime Timestamp { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
    }

    public class ActiveSessionDto
    {
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string LastActivity { get; set; } = string.Empty;
    }

    public class BackgroundTaskDto
    {
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string LastRun { get; set; } = string.Empty;
        public string NextRun { get; set; } = string.Empty;
    }

    public class EndpointHealthDto
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Status { get; set; } = "Unknown"; // Healthy, Degraded, Unhealthy
        public long ResponseTimeMs { get; set; }
        public string LastChecked { get; set; } = string.Empty;
    }

    public class SystemStatus
    {
        public string DatabaseStatus { get; set; } = "Unknown";
        public string StorageStatus { get; set; } = "Unknown";
        public string StorageUsage { get; set; } = string.Empty;
        public string AvailableFreeSpace { get; set; } = string.Empty;
        public double UptimeDays { get; set; }
        public string ServerTime { get; set; } = string.Empty;
    }

    public class AnomalyDto
    {
        public string Type { get; set; } = string.Empty; // e.g., "HighDifference", "MissingData", "OrphanedUser"
        public string Severity { get; set; } = "Warning"; // Info, Warning, Critical
        public string Description { get; set; } = string.Empty;
        public string RelatedEntityId { get; set; } = string.Empty;
        public DateTime DetectedAt { get; set; }
    }

    public class LogEntryDto
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Exception { get; set; } = string.Empty;
    }

    public class DatabaseMetrics
    {
        public string TotalSize { get; set; } = string.Empty;
        public long TotalSizeRaw { get; set; }
        public long FreeSpaceRaw { get; set; }
        public int ConnectionCount { get; set; }
        public long TotalRows { get; set; }
    }

    public class TableStatDto
    {
        public string TableName { get; set; } = string.Empty;
        public long RowCount { get; set; }
        public string Size { get; set; } = string.Empty;
    }

    public class BackupFileDto
    {
        public string FileName { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public string SizePretty { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class LogQueryDto
    {
        public string? Level { get; set; }
        public string? SearchTerm { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}
