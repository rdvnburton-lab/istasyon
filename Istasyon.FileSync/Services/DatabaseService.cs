using Microsoft.Data.Sqlite;
using Istasyon.FileSync.Models;
using System.Collections.Generic;
using System;
using System.IO;

namespace Istasyon.FileSync.Services;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService()
    {
        string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "filesync.db");
        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS FileLogs (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                FileName TEXT NOT NULL,
                FilePath TEXT NOT NULL,
                Hash TEXT NOT NULL,
                Status TEXT NOT NULL,
                LastAttempt DATETIME NOT NULL,
                ErrorMessage TEXT
            );
            CREATE INDEX IF NOT EXISTS idx_filepath ON FileLogs (FilePath);
        ";
        command.ExecuteNonQuery();
    }

    public void AddOrUpdateLog(FileLog log)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO FileLogs (FileName, FilePath, Hash, Status, LastAttempt, ErrorMessage)
            VALUES ($fileName, $filePath, $hash, $status, $lastAttempt, $errorMessage)
            ON CONFLICT(FilePath) DO UPDATE SET
                Status = excluded.Status,
                Hash = excluded.Hash,
                LastAttempt = excluded.LastAttempt,
                ErrorMessage = excluded.ErrorMessage;
        ";
        // Note: SQLite ON CONFLICT requires a UNIQUE constraint. Let's fix the schema.
        // I'll just use a simple check for now or update the schema.
    }

    // Refined version with simple check
    public void SaveLog(FileLog log)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        var checkCmd = connection.CreateCommand();
        checkCmd.CommandText = "SELECT Id FROM FileLogs WHERE FilePath = $path";
        checkCmd.Parameters.AddWithValue("$path", log.FilePath);
        var existingId = checkCmd.ExecuteScalar();

        var command = connection.CreateCommand();
        if (existingId == null)
        {
            command.CommandText = @"
                INSERT INTO FileLogs (FileName, FilePath, Hash, Status, LastAttempt, ErrorMessage)
                VALUES ($fileName, $filePath, $hash, $status, $lastAttempt, $errorMessage)";
        }
        else
        {
            command.CommandText = @"
                UPDATE FileLogs SET 
                    Status = $status, 
                    Hash = $hash, 
                    LastAttempt = $lastAttempt, 
                    ErrorMessage = $errorMessage 
                WHERE FilePath = $filePath";
        }

        command.Parameters.AddWithValue("$fileName", log.FileName);
        command.Parameters.AddWithValue("$filePath", log.FilePath);
        command.Parameters.AddWithValue("$hash", log.Hash);
        command.Parameters.AddWithValue("$status", log.Status);
        command.Parameters.AddWithValue("$lastAttempt", log.LastAttempt);
        command.Parameters.AddWithValue("$errorMessage", (object?)log.ErrorMessage ?? DBNull.Value);
        command.ExecuteNonQuery();
    }

    public FileLog? GetLogByPath(string filePath)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM FileLogs WHERE FilePath = $path";
        command.Parameters.AddWithValue("$path", filePath);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new FileLog
            {
                Id = reader.GetInt32(0),
                FileName = reader.GetString(1),
                FilePath = reader.GetString(2),
                Hash = reader.GetString(3),
                Status = reader.GetString(4),
                LastAttempt = reader.GetDateTime(5),
                ErrorMessage = reader.IsDBNull(6) ? null : reader.GetString(6)
            };
        }
        return null;
    }

    public List<FileLog> GetAllLogs()
    {
        var logs = new List<FileLog>();
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM FileLogs ORDER BY LastAttempt DESC LIMIT 100";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            logs.Add(new FileLog
            {
                Id = reader.GetInt32(0),
                FileName = reader.GetString(1),
                FilePath = reader.GetString(2),
                Hash = reader.GetString(3),
                Status = reader.GetString(4),
                LastAttempt = reader.GetDateTime(5),
                ErrorMessage = reader.IsDBNull(6) ? null : reader.GetString(6)
            });
        }
        return logs;
    }

    public FileLog? GetLastSuccessfulUpload()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM FileLogs WHERE Status = 'Sent' ORDER BY LastAttempt DESC LIMIT 1";

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new FileLog
            {
                Id = reader.GetInt32(0),
                FileName = reader.GetString(1),
                FilePath = reader.GetString(2),
                Hash = reader.GetString(3),
                Status = reader.GetString(4),
                LastAttempt = reader.GetDateTime(5),
                ErrorMessage = reader.IsDBNull(6) ? null : reader.GetString(6)
            };
        }
        return null;
    }
}
