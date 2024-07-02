using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace meteor.Interfaces;

public interface IAutoSaveService
{
    Task InitializeAsync(string filePath, string content);
    Task SaveBackupAsync(string content);
    Task<string?> RestoreFromBackupAsync(string? backupId = null);
    Task SaveAsync(string content);
    Task CleanupAsync(string? backupId = null);
    Task<IEnumerable<BackupInfo>> GetBackupsAsync();
    Task SetBackupIntervalAsync(TimeSpan interval);
    Task<TimeSpan> GetBackupIntervalAsync();
    Task SetBackupRetentionPolicyAsync(TimeSpan maxAge, int maxCount);
    Task<(TimeSpan MaxAge, int MaxCount)> GetBackupRetentionPolicyAsync();

    bool HasBackup { get; }
    string? OriginalFilePath { get; }
    event EventHandler<BackupCreatedEventArgs> BackupCreated;
    event EventHandler<BackupRestoredEventArgs> BackupRestored;
}

public class BackupInfo
{
    public string Id { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long SizeInBytes { get; set; }
}

public class BackupCreatedEventArgs : EventArgs
{
    public BackupInfo BackupInfo { get; set; } = new();
}

public class BackupRestoredEventArgs : EventArgs
{
    public BackupInfo BackupInfo { get; set; } = new();
    public bool Success { get; set; }
}