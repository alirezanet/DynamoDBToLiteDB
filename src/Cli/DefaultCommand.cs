using System.Text;
using CliFx;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using LiteDB;

namespace DynamoDBToLiteDB.Cli;

[Command(Description = "accepts a manifest-summary.json S3 url or file and exports whitelisted fields to csv")]
public class DefaultCommand : ICommand
{
    [CommandParameter(0, Name = "manifest-summary", Description = "file path or S3 Url of manifest-summary.json")]
    public string ManifestSummary { get; set; } = string.Empty;

    [CommandOption("output", 'o', Description = "output file path")]
    public FileInfo Output { get; set; } = new("./lite.db");

    [CommandOption("backup-path", 'b', Description = "path to store downloaded backup files")]
    public string BackupPath { get; set; } = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()[..5]);

    [CommandOption("clean", Description = "removed downloaded backup files after processing has finished")]
    public bool AutoCleanUp { get; set; } = true;

    [CommandOption("collection-name", 'c', Description = "Collection name inside the database.")]
    public string CollectionName { get; set; } = "default";

    [CommandOption("Journal", 'j', Description = "Enable LiteDB journaling to ensure data integrity during operations. Disabling may improve performance for bulk imports but increases risk of data loss if interrupted.")]
    public bool Journal { get; set; } = true;

    [CommandOption("Password", 'p', Description = "Allow you to Set a password for the LiteDB database")]
    public string? Password { get; set; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        if (Output.Exists)
        {
            await console.Output.WriteLineAsync("The database file already exists. do you want to continue? [y/n]");
            var key = await console.Input.ReadLineAsync();
            if (key is "n" or "N" or "no" or "NO")
                return;
        }

        var paths = await GetFilePaths();
        var connectionString = GetConnectionString();
        using var db = new LiteDatabase(connectionString);
        {
            var col = db.GetCollection<BsonDocument>(CollectionName);

            foreach (var file in paths)
                await LightDbWriter.SaveToDb(file, col);
        }

        if (AutoCleanUp)
        {
            await console.Output.WriteLineAsync("removing downloaded files");
            Directory.Delete(BackupPath, true);
        }

        await console.Output.WriteLineAsync(
            $"✅ Successfully done.\nCollectionName: {CollectionName}\nOutputFile: {Output}");
    }

    private string GetConnectionString()
    {
        var sb = new StringBuilder();
        sb.Append($"Filename='{Output.FullName}'; ");
        sb.Append($"Journal={(Journal ? "true" : "false")}; ");
        if (Password is not null) sb.Append($"Password='{Password}';");
        return sb.ToString();
    }

    private async Task<string[]> GetFilePaths()
    {
        string[] paths;
        if (File.Exists(ManifestSummary))
            paths = await S3Helper.DownloadBackupFromFileAsync(ManifestSummary, BackupPath);
        else if (Uri.TryCreate(ManifestSummary, UriKind.Absolute, out var url))
            paths = await S3Helper.DownloadBackupFromUrlAsync(url, BackupPath);
        else
            throw new CommandException("the manifest-summary.json file not found");

        return paths;
    }
}