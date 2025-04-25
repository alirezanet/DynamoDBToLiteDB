using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using LiteDB;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace DynamoDBToLiteDB;

public static class LightDbWriter
{
    public static async Task SaveToDb(string file, ILiteCollection<BsonDocument> collection)
    {
        Console.WriteLine($"Saving '{file}'");
        var buffer = new List<BsonDocument>();
        await foreach (var line in File.ReadLinesAsync(file))
        {
            var outer = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, AttributeValue>>>(line)!;
            var rawMap = outer["Item"];
            var cleanDoc = Document.FromAttributeMap(rawMap);
            var normalJson = cleanDoc.ToJson();
            var doc = LiteDB.JsonSerializer.Deserialize(normalJson).AsDocument;
            buffer.Add(doc.AsDocument);
        }

        collection.InsertBulk(buffer, buffer.Count);
    }
}