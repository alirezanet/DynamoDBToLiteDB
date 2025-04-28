using System.Globalization;
using System.Text;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using CsvHelper;
using CsvHelper.Configuration;
using LiteDB;

namespace DynamoDBToLiteDB.Cli;

[Command("ToCsv", Description = "Converts LiteDB data to CSV (or any-delimited) format.")]
public class ToCsvCommand : ICommand
{
    [CommandParameter(0, Name = "Whitelist-fields",
        Description = "Space-separated whitelisted field names to export")]
    public List<string> Whitelist { get; set; } = [];

    [CommandOption("db", 'd', Description = "Database file path")]
    public FileInfo DbPath { get; set; } = new("./lite.db");

    [CommandOption("output", 'o', Description = "Output file path")]
    public FileInfo Output { get; set; } = new("./output.csv");

    [CommandOption("batch", 'b', Description = "Processor batch size")]
    public int BatchSize { get; set; } = 10_000;

    [CommandOption("where", 'w',
        Description = "SQL-like WHERE filter, e.g. IsActive=true")]
    public string Where { get; set; } = "0=0";

    [CommandOption("collection-name", 'c', Description = "Collection name inside the DB")]
    public string CollectionName { get; set; } = "default";

    [CommandOption("password", 'p', Description = "LiteDB database password")]
    public string? Password { get; set; }

    [CommandOption("separator", 's', Description = "Field separator")]
    public string Separator { get; set; } = ",";

    public async ValueTask ExecuteAsync(IConsole console)
    {
        if (!DbPath.Exists)
        {
            await console.Error.WriteLineAsync($"❌ DB not found: {DbPath.FullName}");
            return;
        }

        Output.Directory?.Create();
        await using var fs = File.Open(Output.FullName, FileMode.Create, FileAccess.Write, FileShare.None);
        await using var writer = new StreamWriter(fs, new UTF8Encoding(false), 1 << 20);

        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
            NewLine = Environment.NewLine,
            Delimiter = Separator
        };
        await using var csv = new CsvWriter(writer, csvConfig);

        // header
        foreach (var f in Whitelist)
            csv.WriteField(f);
        await csv.NextRecordAsync();

        using var db = new LiteDatabase(GetConnectionString());
        var reader = db.Execute($"SELECT $ FROM {CollectionName} WHERE {Where}");

        var counter = 0;
        while (reader.Read())
        {
            var doc = reader.Current.AsDocument;
            foreach (var f in Whitelist)
            {
                doc.TryGetValue(f, out var v);
                csv.WriteField(v?.ToString() ?? "");
            }

            await csv.NextRecordAsync();

            counter++;
            if (counter % BatchSize == 0)
            {
                await writer.FlushAsync();
                await console.Output.WriteLineAsync($"⏳ Flushed {counter:N0} rows…");
            }
        }

        await writer.FlushAsync();
        if (counter > 0)
            await console.Output.WriteLineAsync($"✅ Exported {counter:N0} rows → {Output.FullName}");
        else
            await console.Error.WriteLineAsync("⚠️ No data matched your filter.");
    }

    private string GetConnectionString()
    {
        var sb = new StringBuilder($"Filename={DbPath.FullName};");
        if (!string.IsNullOrEmpty(Password))
            sb.Append($"Password='{Password}';");
        return sb.ToString();
    }
}