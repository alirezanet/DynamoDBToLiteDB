using System.IO.Compression;
using System.Text.RegularExpressions;
using Amazon.S3;
using Amazon.S3.Model;

namespace DynamoDBBackupToLiteDb;

public static class S3Helper
{
    public static async Task<string[]> DownloadBackupFromFileAsync(string manifestSummaryFilePath,
        string outputFolderPath)
    {
        Console.WriteLine("Downloading manifest item ...");
        var manifestSummary = await ManifestSummary.ParseFromFileAsync(manifestSummaryFilePath);
        var fileUrls = await GetManifestItemsAsync(manifestSummary);
        var filePaths =
            await DownloadAndExtractFilesAsync(manifestSummary.S3Bucket, fileUrls, outputFolderPath);
        return filePaths;
    }

    public static async Task<string[]> DownloadBackupFromUrlAsync(Uri manifestSummaryUrl,
        string backupFolder)
    {
        Console.WriteLine("Downloading manifest-summary.json");
        var manifestSummary = await GetManifestSummaryAsync(manifestSummaryUrl);
        Console.WriteLine("Downloading manifest items ...");
        var fileUrls = await GetManifestItemsAsync(manifestSummary);
        var filePaths =
            await DownloadAndExtractFilesAsync(manifestSummary.S3Bucket, fileUrls, backupFolder);
        return filePaths;
    }

    public static async Task<ManifestSummary> GetManifestSummaryAsync(Uri manifestSummaryUrl)
    {
        using var s3Client = new AmazonS3Client();
        var regex = new Regex(@"(?:s3:\/\/)(.[^/]+)\/(.+manifest-summary.json)", RegexOptions.Compiled);
        var url = regex.Match(manifestSummaryUrl.ToString());
        if (!url.Success) throw new ArgumentException("S3 url is not valid");

        var getObjectRequest = new GetObjectRequest
        {
            BucketName = url.Groups[1].Value,
            Key = url.Groups[2].Value
        };
        using var response = await s3Client.GetObjectAsync(getObjectRequest);
        await using var responseStream = response.ResponseStream;
        using var reader = new StreamReader(responseStream);
        var json = await reader.ReadToEndAsync();
        var manifestSummary = ManifestSummary.Parse(json);
        return manifestSummary;
    }


    public static async Task<List<ManifestItem>> GetManifestItemsAsync(ManifestSummary manifestSummary)
    {
        using var s3Client = new AmazonS3Client();
        var getObjectRequest = new GetObjectRequest
        {
            BucketName = manifestSummary.S3Bucket,
            Key = manifestSummary.ManifestFilesS3Key
        };
        var manifestItems = new List<ManifestItem>();
        using var response = await s3Client.GetObjectAsync(getObjectRequest);
        await using var responseStream = response.ResponseStream;
        using var reader = new StreamReader(responseStream);
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            var manifestItem = ManifestItem.ParseFromJson(line!);
            manifestItems.Add(manifestItem);
        }

        return manifestItems;
    }

    public static async Task<string[]> DownloadAndExtractFilesAsync(string bucketName,
        IEnumerable<ManifestItem> manifestItems, string destinationFolder)
    {
        using var s3Client = new AmazonS3Client();

        Console.WriteLine($"backup folder: '{destinationFolder}' ");

        var tasks = manifestItems.Select(manifestItem =>
            DownloadAndExtractFileAsync(bucketName, destinationFolder, manifestItem, s3Client)).ToList();
        var filePaths = await Task.WhenAll(tasks);
        return filePaths;
    }

    private static async Task<string> DownloadAndExtractFileAsync(
        string bucketName,
        string destinationFolder,
        ManifestItem manifestItem,
        IAmazonS3 s3Client)
    {
        Console.WriteLine($"Downloading backup '{manifestItem.DataFileS3Key}'");
        var getObjectRequest = new GetObjectRequest
        {
            BucketName = bucketName,
            Key = manifestItem.DataFileS3Key
        };

        if (!Directory.Exists(destinationFolder)) Directory.CreateDirectory(destinationFolder);
        var filePath = Path.Combine(destinationFolder,
            Path.GetFileNameWithoutExtension(manifestItem.DataFileS3Key));

        using var response = await s3Client.GetObjectAsync(getObjectRequest);
        await using var responseStream = response.ResponseStream;
        await using var gzipStream = new GZipStream(responseStream, CompressionMode.Decompress);
        await using var fileStream = File.Create(filePath);
        await gzipStream.CopyToAsync(fileStream);
        fileStream.Close();

        return filePath;
    }
}