using Microsoft.Data.SqlClient;

namespace HRMS.Services;

public class BackupService
{
    private readonly string _connectionString;

    public BackupService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task BackupAsync()
    {
        var backupDirectory = Path.Combine(AppContext.BaseDirectory, "Backups");
        Directory.CreateDirectory(backupDirectory);

        var backupPath = Path.Combine(backupDirectory, $"HRMS_{DateTime.Now:yyyyMMdd_HHmmss}.bak");
        var escapedBackupPath = backupPath.Replace("'", "''", StringComparison.Ordinal);
        var commandText = $"BACKUP DATABASE [HRMS] TO DISK = N'{escapedBackupPath}' WITH COPY_ONLY, INIT, FORMAT";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using (var command = new SqlCommand(commandText, connection))
        {
            command.CommandTimeout = 300;
            await command.ExecuteNonQueryAsync();
        }

        var filesToDelete = new DirectoryInfo(backupDirectory)
            .GetFiles("HRMS_*.bak")
            .OrderByDescending(file => file.CreationTimeUtc)
            .Skip(5);

        foreach (var file in filesToDelete)
        {
            file.Delete();
        }
    }
}
