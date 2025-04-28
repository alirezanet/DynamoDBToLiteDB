using System.Text;
using CliFx;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using LiteDB;

namespace DynamoDBToLiteDB.Cli;

[Command(Description = "Accepts a manifest-summary.json S3 URL or file and saves it to a LiteDB database")]
public class DefaultCommand : ICommand
{
    [CommandParameter(0, Name = "manifest-summary",
        Description = "File path or S3 URL of manifest-summary.json")]
    public string ManifestSummary { get; set; } = string.Empty;

    [CommandOption("output", 'o', Description = "Database file path")]
    public FileInfo Output { get; set; } = new("./lite.db");

    [CommandOption("backup-path", 'b',
        Description = "Path to store downloaded backup files")]
    public string BackupPath { get; set; }
        = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()[..5]);

    [CommandOption("clean", Description = "Remove downloaded backups after processing")]
    public bool AutoCleanUp { get; set; } = true;

    [CommandOption("collection-name", 'c',
        Description = "Collection name inside the database")]
    public string CollectionName { get; set; } = "default";

    [CommandOption("journal", 'j', Description = "Enable LiteDB journaling to ensure data integrity during operations. Disabling may improve performance for bulk imports but increases risk of data loss if interrupted.")]
    public bool Journal { get; set; } = true;

    [CommandOption("password", 'p',
        Description = "Password for the LiteDB database")]
    public string? Password { get; set; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        if (Output.Exists)
        {
            await console.Output.WriteLineAsync(
                "The database file already exists. Continue? [y/N]");
            var answer = (await console.Input.ReadLineAsync())?.Trim().ToLower();
            if (answer is "n" or "no" or "" or null)
                return;
        }

        // Ensure output folder
        Output.Directory?.Create();

        // Get manifest files
        var paths = await GetFilePaths();

        // Open LiteDB
        using var db = new LiteDatabase(GetConnectionString());
        var col = db.GetCollection<BsonDocument>(CollectionName);

        foreach (var file in paths)
            await LightDbWriter.SaveToDb(file, col);

        // Cleanup
        if (AutoCleanUp && Directory.Exists(BackupPath))
        {
            await console.Output.WriteLineAsync("🔄 Removing downloaded files...");
            Directory.Delete(BackupPath, true);
        }

        await console.Output.WriteLineAsync(
            $"✅ Done!\nCollection: {CollectionName}\nDatabase: {Output.FullName}");
    }

    private string GetConnectionString()
    {
        var sb = new StringBuilder()
            .Append($"Filename={Output.FullName};")
            .Append($"Journal={(Journal ? "true" : "false")};");
        if (!string.IsNullOrWhiteSpace(Password))
            sb.Append($"Password='{Password}';");
        return sb.ToString();
    }

    private async Task<string[]> GetFilePaths()
    {
        if (File.Exists(ManifestSummary))
            return await S3Helper.DownloadBackupFromFileAsync(
                ManifestSummary, BackupPath);

        if (Uri.TryCreate(ManifestSummary, UriKind.Absolute, out var url))
            return await S3Helper.DownloadBackupFromUrlAsync(
                url, BackupPath);

        throw new CommandException(
            $"Manifest-summary.json not found or invalid: {ManifestSummary}");
    }
}