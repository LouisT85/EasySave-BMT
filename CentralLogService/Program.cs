using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

string storagePath =
    builder.Configuration["CentralLogs:StoragePath"] ??
    Environment.GetEnvironmentVariable("LOG_STORAGE_PATH") ??
    "/app/logs";

Directory.CreateDirectory(storagePath);

var app = builder.Build();
var fileWriteLock = new object();
var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

app.MapGet("/", () => Results.Ok(new
{
    service = "central-log-service",
    status = "ok",
    storagePath
}));

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/logs", (InboundLogEntry entry) =>
{
    if (entry is null)
    {
        return Results.BadRequest(new { error = "Payload is required." });
    }

    DateTime timestamp = entry.Timestamp == default ? DateTime.Now : entry.Timestamp;
    string outputFilePath = Path.Combine(storagePath, $"{timestamp:yyyy-MM-dd}.json");

    var normalized = entry with
    {
        Timestamp = timestamp,
        MachineName = string.IsNullOrWhiteSpace(entry.MachineName) ? "unknown-machine" : entry.MachineName.Trim(),
        UserName = string.IsNullOrWhiteSpace(entry.UserName) ? "unknown-user" : entry.UserName.Trim(),
        BackupName = entry.BackupName?.Trim() ?? string.Empty,
        SourcePath = entry.SourcePath?.Trim() ?? string.Empty,
        DestinationPath = entry.DestinationPath?.Trim() ?? string.Empty
    };

    lock (fileWriteLock)
    {
        List<InboundLogEntry> entries = new List<InboundLogEntry>();

        if (File.Exists(outputFilePath) && new FileInfo(outputFilePath).Length > 0)
        {
            try
            {
                string existingRaw = File.ReadAllText(outputFilePath);
                entries = JsonSerializer.Deserialize<List<InboundLogEntry>>(existingRaw) ?? new List<InboundLogEntry>();
            }
            catch
            {
                entries = new List<InboundLogEntry>();
            }
        }

        entries.Add(normalized);
        string serialized = JsonSerializer.Serialize(entries, jsonOptions);
        File.WriteAllText(outputFilePath, serialized);
    }

    return Results.Accepted($"/logs/{timestamp:yyyy-MM-dd}", new { file = $"{timestamp:yyyy-MM-dd}.json" });
});

app.Run();

public sealed record InboundLogEntry
{
    public DateTime Timestamp { get; init; }
    public string BackupName { get; init; } = string.Empty;
    public string SourcePath { get; init; } = string.Empty;
    public string DestinationPath { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public long TransferTimeMs { get; init; }
    public long EncryptionTimeMs { get; init; }
    public string MachineName { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
}
