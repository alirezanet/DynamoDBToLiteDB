using System.Text;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using LiteDB;

namespace DynamoDBToLiteDB.Cli;

[Command("ToCsv", Description = "Converts LiteDB data to CSV format.")]
public class LiteDbToCsvCommand : ICommand
{
    [CommandParameter(0, Name = "Whitelist-fields",
        Description = "Space-seperated whitelisted field names to export to CSV")]
    public List<string> Whitelist { get; set; } = [];

    [CommandOption("db", 'd', Description = "Database file path")]
    public FileInfo DbPath { get; set; } = new("./lite.db");

    [CommandOption("output", 'o', Description = "Output file path")]
    public FileInfo Output { get; set; } = new("./output.csv");

    [CommandOption("batch", 'b', Description = "The processor batch size")]
    public int BatchSize { get; set; } = 10_000;

    [CommandOption("where", 'w',
        Description = "SQL-Like Where condition(s) for filtering. e.g Id='ffd1b531644041349327ae03776fa5bc'")]
    public string Where { get; set; } = "0=0";

    [CommandOption("collection-name", 'c', Description = "Collection name inside the database.")]
    public string CollectionName { get; set; } = "default";

    [CommandOption("Password", 'p', Description = "LiteDB database password")]
    public string? Password { get; set; }


    public async ValueTask ExecuteAsync(IConsole console)
    {
        await using var fs = File.OpenWrite(Output.FullName);
        await using var writer = new StreamWriter(fs);
        await writer.WriteLineAsync(string.Join(",", Whitelist));
        var connectionString = GetConnectionString();
        using var db = new LiteDatabase(connectionString);
        var counter = 0;
        var reader = db.Execute($"SELECT $ FROM {CollectionName} WHERE {Where}");

        while (reader.Read())
        {
            var doc = reader.Current.AsDocument;
            counter += 1;
            var row = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var kv in doc)
            {
                if (!Whitelist.Contains(kv.Key)) continue;
                row[kv.Key] = kv.Value?.ToString() ?? "";
            }

            await writer.WriteLineAsync(string.Join(",", Whitelist.Select(w => row.GetValueOrDefault(w, ""))));

            if (counter % BatchSize == 0)
            {
                await console.Output.WriteLineAsync($"Exported {counter} rows.");
                await writer.FlushAsync();
            }
        }

        await writer.FlushAsync();
        if (counter > 1)
            await console.Output.WriteLineAsync($"✅ Successfully exported. {counter} rows.\n{Output.FullName}");
        else
            await console.Error.WriteLineAsync("⚠️ No data found to export.");
    }

    private string GetConnectionString()
    {
        var sb = new StringBuilder();
        sb.Append($"Filename='{DbPath.FullName}'; ");
        if (Password is not null) sb.Append($"Password='{Password}'; ");
        return sb.ToString();
    }
}