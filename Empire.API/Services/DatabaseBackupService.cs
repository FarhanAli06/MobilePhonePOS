using Microsoft.Data.SqlClient;

namespace Empire.API.Services;

public sealed class DatabaseBackupState
{
    public DateTime? LastSuccessfulUtc { get; set; }
    public DateTime? LastAttemptUtc { get; set; }
    public string Status { get; set; } = "Waiting for first automatic backup";
    public string? FileName { get; set; }
}

public sealed class DatabaseBackupService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly DatabaseBackupState _state;
    private readonly ILogger<DatabaseBackupService> _logger;

    public DatabaseBackupService(IConfiguration configuration, DatabaseBackupState state, ILogger<DatabaseBackupService> logger)
    {
        _configuration = configuration;
        _state = state;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunBackupAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task RunBackupAsync(CancellationToken token)
    {
        _state.LastAttemptUtc = DateTime.UtcNow;
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("The database connection is not configured.");
            var builder = new SqlConnectionStringBuilder(connectionString);
            var database = builder.InitialCatalog;
            if (string.IsNullOrWhiteSpace(database)) throw new InvalidOperationException("The database name is missing.");

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(token);
            await using var pathCommand = new SqlCommand("SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000))", connection);
            var backupDirectory = Convert.ToString(await pathCommand.ExecuteScalarAsync(token));
            if (string.IsNullOrWhiteSpace(backupDirectory)) throw new InvalidOperationException("SQL Server did not provide its backup folder.");

            var fileName = $"{database}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.bak";
            var fullPath = Path.Combine(backupDirectory, fileName).Replace("'", "''");
            var safeDatabase = database.Replace("]", "]]", StringComparison.Ordinal);
            await using var backupCommand = new SqlCommand($"BACKUP DATABASE [{safeDatabase}] TO DISK = N'{fullPath}' WITH COPY_ONLY, CHECKSUM, INIT", connection)
            {
                CommandTimeout = 600
            };
            await backupCommand.ExecuteNonQueryAsync(token);
            _state.LastSuccessfulUtc = DateTime.UtcNow;
            _state.FileName = fileName;
            _state.Status = "Successful";
            _logger.LogInformation("Automatic database backup completed: {FileName}", fileName);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception ex)
        {
            _state.Status = "Failed: " + ex.Message;
            _logger.LogError(ex, "Automatic database backup failed");
        }
    }
}
