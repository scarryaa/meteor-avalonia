using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using meteor.Interfaces;

namespace meteor.Services;

public class AutoSaveService : IAutoSaveService
{
    private const string BackupExtension = ".backup";
    private readonly string _backupDirectory;
    private TimeSpan _backupInterval = TimeSpan.FromMinutes(5);
    private TimeSpan _maxBackupAge = TimeSpan.FromDays(7);
    private int _maxBackupCount = 10;

    public AutoSaveService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _backupDirectory = Path.Combine(appDataPath, "meteor", "Backups");
        Directory.CreateDirectory(_backupDirectory);
    }

    public bool HasBackup => Directory.GetFiles(_backupDirectory,
        $"{Path.GetFileNameWithoutExtension(OriginalFilePath)}*{BackupExtension}*").Any();

    public string? OriginalFilePath { get; private set; }

    public event EventHandler<BackupCreatedEventArgs>? BackupCreated;
    public event EventHandler<BackupRestoredEventArgs>? BackupRestored;

    public async Task InitializeAsync(string filePath, string content)
    {
        OriginalFilePath = filePath;
        await SaveBackupAsync(content);
    }

    public async Task SaveBackupAsync(string content)
    {
        if (string.IsNullOrEmpty(OriginalFilePath))
            throw new InvalidOperationException("Original file path is not set.");

        try
        {
            var backupPath = GenerateBackupFilePath(OriginalFilePath);
            await File.WriteAllTextAsync(backupPath, content);

            var backupInfo = new BackupInfo
            {
                Id = Path.GetFileName(backupPath),
                CreatedAt = DateTime.Now,
                SizeInBytes = new FileInfo(backupPath).Length
            };

            BackupCreated?.Invoke(this, new BackupCreatedEventArgs { BackupInfo = backupInfo });

            await EnforceRetentionPolicyAsync();
        }
        catch (Exception ex)
        {
            // Log the error
            Console.WriteLine($"Error saving backup: {ex.Message}");
            throw;
        }
    }

    public async Task<string?> RestoreFromBackupAsync(string? backupId = null)
    {
        var backups = await GetBackupsAsync();
        var backupToRestore = backupId != null
            ? backups.FirstOrDefault(b => b.Id == backupId)
            : backups.MaxBy(b => b.CreatedAt);

        if (backupToRestore == null) return null;

        try
        {
            var content = await File.ReadAllTextAsync(Path.Combine(_backupDirectory, backupToRestore.Id));
            BackupRestored?.Invoke(this, new BackupRestoredEventArgs { BackupInfo = backupToRestore, Success = true });
            return content;
        }
        catch (Exception ex)
        {
            // Log the error
            Console.WriteLine($"Error restoring from backup: {ex.Message}");
            BackupRestored?.Invoke(this, new BackupRestoredEventArgs { BackupInfo = backupToRestore, Success = false });
            throw;
        }
    }

    public async Task SaveAsync(string content)
    {
        if (string.IsNullOrEmpty(OriginalFilePath))
            throw new InvalidOperationException("Original file path is not set.");

        try
        {
            await File.WriteAllTextAsync(OriginalFilePath, content);
        }
        catch (Exception ex)
        {
            // Log the error
            Console.WriteLine($"Error saving file: {ex.Message}");
            throw;
        }
    }

    public async Task CleanupAsync(string? backupId = null)
    {
        if (backupId != null)
        {
            var backupPath = Path.Combine(_backupDirectory, backupId);
            if (File.Exists(backupPath))
                File.Delete(backupPath);
        }
        else
        {
            await EnforceRetentionPolicyAsync();
        }
    }

    public async Task<IEnumerable<BackupInfo>> GetBackupsAsync()
    {
        if (string.IsNullOrEmpty(OriginalFilePath))
            return Enumerable.Empty<BackupInfo>();

        var backupFiles = Directory.GetFiles(_backupDirectory,
            $"{Path.GetFileNameWithoutExtension(OriginalFilePath)}*{BackupExtension}*");
        return backupFiles.Select(f => new BackupInfo
        {
            Id = Path.GetFileName(f),
            CreatedAt = File.GetCreationTime(f),
            SizeInBytes = new FileInfo(f).Length
        });
    }

    public Task SetBackupIntervalAsync(TimeSpan interval)
    {
        _backupInterval = interval;
        return Task.CompletedTask;
    }

    public Task<TimeSpan> GetBackupIntervalAsync()
    {
        return Task.FromResult(_backupInterval);
    }

    public Task SetBackupRetentionPolicyAsync(TimeSpan maxAge, int maxCount)
    {
        _maxBackupAge = maxAge;
        _maxBackupCount = maxCount;
        return Task.CompletedTask;
    }

    public Task<(TimeSpan MaxAge, int MaxCount)> GetBackupRetentionPolicyAsync()
    {
        return Task.FromResult((_maxBackupAge, _maxBackupCount));
    }

    private string GenerateBackupFilePath(string originalFilePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(originalFilePath);
        var extension = Path.GetExtension(originalFilePath);
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

        return Path.Combine(_backupDirectory, $"{fileName}_{timestamp}{BackupExtension}{extension}");
    }

    private async Task EnforceRetentionPolicyAsync()
    {
        var backups = await GetBackupsAsync();
        var orderedBackups = backups.OrderByDescending(b => b.CreatedAt).ToList();

        // Remove old backups
        foreach (var backup in orderedBackups.Where(b => b.CreatedAt < DateTime.Now - _maxBackupAge))
            await CleanupAsync(backup.Id);

        // Remove excess backups
        foreach (var backup in orderedBackups.Skip(_maxBackupCount)) await CleanupAsync(backup.Id);
    }
}