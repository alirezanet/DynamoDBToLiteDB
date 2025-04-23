using System.Text.Json;

namespace DynamoDBBackupToLiteDb;

public class ManifestSummary
{
    public required string Version { get; set; }
    public required string ExportArn { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public required string TableArn { get; set; }
    public required string TableId { get; set; }
    public DateTime ExportTime { get; set; }
    public required string S3Bucket { get; set; }
    public required string S3Prefix { get; set; }
    public required string S3SseAlgorithm { get; set; }
    public required string S3SseKmsKeyId { get; set; }
    public required string ManifestFilesS3Key { get; set; }
    public long BilledSizeBytes { get; set; }
    public int ItemCount { get; set; }
    public required string OutputFormat { get; set; }

    public static async Task<ManifestSummary> ParseFromFileAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<ManifestSummary>(json, new JsonSerializerOptions
               {
                   PropertyNameCaseInsensitive = true
               })
               ?? throw new InvalidOperationException("invalid manifest summary");
    }

    public static ManifestSummary Parse(string jsonString)
    {
        return JsonSerializer.Deserialize<ManifestSummary>(jsonString, new JsonSerializerOptions
               {
                   PropertyNameCaseInsensitive = true
               })
               ?? throw new InvalidOperationException("invalid manifest summary");
    }
}