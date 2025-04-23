using System.Text.Json;

namespace DynamoDBBackupToLiteDb;

public class ManifestItem
{
    public int ItemCount { get; set; }
    public required string Md5Checksum { get; set; }
    public required string Etag { get; set; }
    public required string DataFileS3Key { get; set; }

    public static ManifestItem ParseFromJson(string json)
    {
        return JsonSerializer.Deserialize<ManifestItem>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("can't parse ManifestItem");
    }
}