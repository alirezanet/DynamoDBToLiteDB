﻿
![GitHub](https://img.shields.io/github/license/alirezanet/dynamodbtolitedb) 
![Nuget](https://img.shields.io/nuget/dt/dynamodbtolitedb?color=%239100ff)
![Nuget](https://img.shields.io/nuget/v/dynamodbtolitedb?label=stable) 
[![NuGet version](https://img.shields.io/nuget/v/dynamodbtolitedb.svg?style=flat-square)](https://www.nuget.org/packages/dynamodbtolitedb/)

# DynamoDbToLiteDb

A small CLI tool to turn a DynamoDB backup into a local LiteDB file—and optionally CSV—so you can query or export data
without incurring large scan costs.

## Why use this

- **Lower cost**: Downloading a backup from S3 avoids DynamoDB scans and high read-capacity consumption.
- **Local querying**: Inspect or filter data in LiteDB without hitting AWS.
- **CSV export**: Extract only the fields you need for reports or sharing.

## Prerequisites

- Export your DynamoDB table to an S3 bucket
- Copy the S3 path to `manifest-summary.json` (or download that file locally)
- AWS credentials/configured with S3 “GetObject” access on your machine

## Installation

```bash
dotnet tool install -g DynamoDbToLiteDb

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



| Flag                      | Description                                                | Default                                |
|----------------------------|------------------------------------------------------------|----------------------------------------|
| `-o`, `--output`           | Output database file path                                  | `./lite.db`                            |
| `-b`, `--backup-path`      | Temp folder to store downloaded backup shards              | `C:\Users\AliReza\AppData\Local\Temp\c5077` |
| `--clean`                  | Delete downloaded backup files after processing           | `true`                                 |
| `-c`, `--collection-name`  | Collection name inside the LiteDB database                 | `default`                              |
| `-j`, `--journal`          | Enable LiteDB journaling for data integrity (disable for faster bulk imports) | `true` |
| `-p`, `--password`         | Password to encrypt the LiteDB database                    |                                        |
| `-h`, `--help`             | Show help text                                             |                                        |
| `--version`                | Show version information                                  |                                        |


### Export CSV from LiteDB

```bash
dynamoDbToLiteDb tocsv <fields...> [options]
```

- `<fields...>` — space-separated field names to include in CSV

**Options**

| Flag                      | Description                              | Default         |
|----------------------------|------------------------------------------|-----------------|
| `-d`, `--db`               | Database file path                      | `./lite.db`     |
| `-o`, `--output`           | CSV output file path                    | `./output.csv`  |
| `-b`, `--batch`            | Processor batch size                    | `10000`         |
| `-w`, `--where`            | SQL-like WHERE condition(s) for filtering | `0=0`          |
| `-c`, `--collection-name`  | Collection name inside the database     | `default`       |
| `-p`, `--password`         | LiteDB database password                |                 |
| `-h`, `--help`             | Shows help text                         |                 |


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
