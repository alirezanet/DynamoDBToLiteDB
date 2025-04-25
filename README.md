# DynamoDbToLiteDb

A small CLI tool to turn a DynamoDB backup into a local LiteDB file—and optionally CSV—so you can query or export data
without incurring large scan costs.

## Why use this

- **Lower cost**: Downloading a backup from S3 avoids DynamoDB scans and high read-capacity consumption.
- **Local querying**: Inspect or filter data in LiteDB without hitting AWS.
- **CSV export**: Extract only the fields you need for reports or sharing.

## Prerequisites

- Backup your DynamoDB table to an S3 bucket
- Copy the S3 path to `manifest-summary.json` (or download that file locally)
- AWS credentials/configured with S3 “GetObject” access on your machine

## Installation

```bash
# Not deployed yet
# dotnet tool install -g dynamoDbToLiteDb

# Or build locally
git clone https://github.com/your-org/dynamoDbToLiteDb.git
cd dynamoDbToLiteDb
dotnet pack
dotnet tool install --global --add-source ./nupkg DynamoDBToLiteDB
```

## Usage

```
dynamoDbToLiteDb [command] [options]
```

### Import backup into LiteDB

```bash
dynamoDbToLiteDb <manifest-summary> [options]
```

- `<manifest-summary>` — local path or S3 URL to `manifest-summary.json`

**Options**

| Flag                      | Description                       | Default                 |
|---------------------------|-----------------------------------|-------------------------|
| `-o`, `--output`          | Output `.db` path                 | `./lite.db`             |
| `-b`, `--backup-path`     | Temp folder for downloaded shards | OS temp + `/d2l-backup` |
| `--clean`                 | Delete temp shards after import   | `true`                  |
| `-c`, `--collection-name` | LiteDB collection name            | `default`               |
| `-h`, `--help`            | Show help                         |                         |

### Export CSV from LiteDB

```bash
dynamoDbToLiteDb tocsv <fields...> [options]
```

- `<fields...>` — space-separated field names to include in CSV

**Options**

| Flag                      | Description           | Default        |
|---------------------------|-----------------------|----------------|
| `-d`, `--db`              | LiteDB file           | `./lite.db`    |
| `-o`, `--output`          | CSV output path       | `./output.csv` |
| `-b`, `--batch`           | Rows per batch flush  | `10000`        |
| `-w`, `--where`           | SQL-like WHERE filter | `0=0`          |
| `-c`, `--collection-name` | Collection name       | `default`      |
| `-h`, `--help`            | Show help             |                |

## Examples

1. **Import S3 backup**
   ```bash
   dynamoDbToLiteDb s3://my-bucket/backups/manifest-summary.json \
     --output data/app.db \
     --backup-path ./tmp \
     --clean
   ```

2. **Export selected fields**
   ```bash
   dynamoDbToLiteDb tocsv UserId Name Email \
     --db data/app.db \
     --output users.csv \
     --where "IsActive=true"
   ```

## Contributing

1. Fork the repo
2. Create a branch (`git checkout -b my-change`)
3. Commit & push
4. Open a pull request

## License

MIT