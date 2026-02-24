using System.Text;
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
        string serializedEntry = JsonSerializer.Serialize(normalized, jsonOptions);
        bool appended = TryAppendEntryToJsonArray(outputFilePath, serializedEntry);

        if (!appended)
        {
            // Fallback for unexpected file format: rebuild from a parsed list.
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

        app.Logger.LogInformation(
            "Centralized log entry written to {FilePath}.",
            outputFilePath);
    }

    return Results.Accepted($"/logs/{timestamp:yyyy-MM-dd}", new { file = $"{timestamp:yyyy-MM-dd}.json" });
});

app.Run();

static bool TryAppendEntryToJsonArray(string outputFilePath, string serializedEntry)
{
    if (!File.Exists(outputFilePath) || new FileInfo(outputFilePath).Length == 0)
    {
        File.WriteAllText(outputFilePath, "[\n" + serializedEntry + "\n]");
        return true;
    }

    using var fs = new FileStream(outputFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

    long lastNonWhitespacePos = fs.Length - 1;
    while (lastNonWhitespacePos >= 0)
    {
        fs.Seek(lastNonWhitespacePos, SeekOrigin.Begin);
        int b = fs.ReadByte();
        if (b < 0) return false;
        if (!char.IsWhiteSpace((char)b)) break;
        lastNonWhitespacePos--;
    }

    if (lastNonWhitespacePos < 0)
    {
        return false;
    }

    fs.Seek(lastNonWhitespacePos, SeekOrigin.Begin);
    int lastChar = fs.ReadByte();
    if (lastChar != ']')
    {
        return false;
    }

    long beforeClosingBracketPos = lastNonWhitespacePos - 1;
    while (beforeClosingBracketPos >= 0)
    {
        fs.Seek(beforeClosingBracketPos, SeekOrigin.Begin);
        int b = fs.ReadByte();
        if (b < 0) return false;
        if (!char.IsWhiteSpace((char)b)) break;
        beforeClosingBracketPos--;
    }

    if (beforeClosingBracketPos < 0)
    {
        return false;
    }

    fs.Seek(beforeClosingBracketPos, SeekOrigin.Begin);
    int previousChar = fs.ReadByte();
    bool isEmptyArray = previousChar == '[';

    // Remove trailing whitespace + closing bracket.
    fs.SetLength(lastNonWhitespacePos);
    fs.Seek(lastNonWhitespacePos, SeekOrigin.Begin);

    using var writer = new StreamWriter(fs, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), 1024, leaveOpen: true);
    writer.Write(isEmptyArray ? "\n" : ",\n");
    writer.Write(serializedEntry);
    writer.Write("\n]");
    writer.Flush();
    fs.Flush(true);

    return true;
}

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
