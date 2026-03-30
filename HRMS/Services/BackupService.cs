namespace HRMS.Services;

public class BackupService
{
    public Task BackupAsync()
    {
        var sourcePath = Path.Combine(FileSystem.AppDataDirectory, "hrms.db");
        if (!File.Exists(sourcePath))
        {
            return Task.CompletedTask;
        }

        var backupDirectory = Path.Combine(FileSystem.AppDataDirectory, "Backups");
        Directory.CreateDirectory(backupDirectory);

        var backupFileName = $"hrms_backup_{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}.db";
        var backupPath = Path.Combine(backupDirectory, backupFileName);

        File.Copy(sourcePath, backupPath, overwrite: false);

        var oldBackups = Directory.GetFiles(backupDirectory, "hrms_backup_*.db")
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .Skip(5)
            .ToList();

        foreach (var backup in oldBackups)
        {
            try
            {
                backup.Delete();
            }
            catch
            {
            }
        }

        return Task.CompletedTask;
    }
}
