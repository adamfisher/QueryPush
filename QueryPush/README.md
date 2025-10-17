# QueryPush

**Cross-platform database query scheduler with cron-based execution**

QueryPush is a .NET 8 application that executes scheduled database queries and pushes results to HTTP endpoints. It supports multiple database providers, flexible scheduling with cron expressions, and comprehensive error handling with retry logic.

## Features

- **Multiple Database Providers**: Native support for SQL Server, MySQL, PostgreSQL, Oracle, SQLite, and ODBC
- **Flexible Scheduling**: Cron-based query scheduling with support for complex time expressions
- **HTTP Integration**: POST query results to any HTTP endpoint with customizable headers and retry logic
- **Cross-Platform**: Runs on Windows, Linux, and macOS
- **Service Support**: Can run as a Windows Service or Linux systemd daemon
- **Robust Error Handling**: Configurable retry strategies with exponential backoff
- **Alert System**: Slack and email notifications for query failures
- **Variable Substitution**: Dynamic query variables with date/time support
- **JSON Payload Formats**: JsonArray or JsonLines output formats
- **Connection Validation**: Validates all database connections at startup

## Quick Start

### Installation

#### Option 1: Download Pre-built Binaries

Pre-built self-contained binaries are available for all platforms in the [GitHub Releases](https://github.com/adamfisher/QueryPush/releases) tab.

**Available platforms:**
- **Windows**: `win-x64`, `win-x86`, `win-arm64`
- **Linux**: `linux-x64`, `linux-arm64`, `linux-arm`
- **macOS**: `osx-x64` (Intel), `osx-arm64` (Apple Silicon)

Each release contains a platform-specific executable along with required native dependencies (SQLite, SQL Server drivers). Simply download the archive for your platform, extract it, configure `appsettings.json`, and run.

**Note:** The executables are self-contained and include the .NET runtime - no additional dependencies need to be installed.

#### Option 2: Build from Source

1. Clone the repository
2. Configure your databases and queries in `appsettings.json`
3. Run the application:
   ```bash
   dotnet run
   ```

### Running as a Service

**Windows:**
```powershell
.\QueryPush.exe --service
```

**Linux:**
```bash
./QueryPush --service
```

## Configuration

Configuration is managed through `appsettings.json`. The file supports multiple sections:

- **databases**: Define database connections with providers
- **endpoints**: Configure HTTP endpoints for data delivery
- **queries**: Define scheduled queries with cron expressions
- **alerts**: Configure Slack and email notifications
- **logging**: Configure log rotation and retention

## Database Providers

QueryPush supports multiple database providers through a unified configuration interface. Each database requires a `provider` and `connectionString`.

### Supported Providers

| Provider | Value | Description |
|----------|-------|-------------|
| SQL Server | `sqlserver` | Microsoft SQL Server (native) |
| MySQL | `mysql` | MySQL and MariaDB (native) |
| PostgreSQL | `postgres` or `postgresql` | PostgreSQL (native) |
| Oracle | `oracle` | Oracle Database (native) |
| SQLite | `sqlite` | SQLite file-based database |
| ODBC | `odbc` | Any ODBC-compatible database |

### Database Configuration Examples

#### SQL Server (Native)
```json
{
  "name": "SqlServerDb",
  "provider": "sqlserver",
  "connectionString": "Server=localhost;Database=MyApp;Integrated Security=true;TrustServerCertificate=true;"
}
```

**Common SQL Server Connection String Options:**
- Integrated Security: `Integrated Security=true` (Windows Auth)
- SQL Auth: `User Id=myuser;Password=mypass`
- Encryption: `Encrypt=true;TrustServerCertificate=true`
- Connection Timeout: `Connect Timeout=30`

#### MySQL (Native)
```json
{
  "name": "MySqlDb",
  "provider": "mysql",
  "connectionString": "Server=localhost;Database=inventory;User=app_user;Password=secure_password;Port=3306;"
}
```

**Common MySQL Connection String Options:**
- Port: `Port=3306` (default)
- SSL: `SslMode=Required`
- Charset: `CharSet=utf8mb4`
- Connection Timeout: `ConnectionTimeout=30`

#### PostgreSQL (Native)
```json
{
  "name": "PostgresDb",
  "provider": "postgres",
  "connectionString": "Host=localhost;Database=analytics;Username=postgres;Password=pg_password;Port=5432;"
}
```

**Common PostgreSQL Connection String Options:**
- Port: `Port=5432` (default)
- SSL: `SSL Mode=Require`
- Timeout: `Timeout=30`
- Pooling: `Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100`

#### Oracle (Native)
```json
{
  "name": "OracleDb",
  "provider": "oracle",
  "connectionString": "Data Source=localhost:1521/XE;User Id=hr;Password=oracle_pass;"
}
```

**Common Oracle Connection String Options:**
- TNS: `Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=orcl)))`
- Simple: `Data Source=localhost:1521/XE`
- Connection Timeout: `Connection Timeout=60`

#### SQLite (Native)
```json
{
  "name": "SqliteDb",
  "provider": "sqlite",
  "connectionString": "Data Source=./data/app.db;"
}
```

**Common SQLite Connection String Options:**
- In-Memory: `Data Source=:memory:`
- Read-Only: `Mode=ReadOnly`
- Shared Cache: `Cache=Shared`
- Foreign Keys: `Foreign Keys=True`

#### ODBC (Legacy/Universal)
```json
{
  "name": "LegacyOdbcDb",
  "provider": "odbc",
  "connectionString": "Driver={ODBC Driver 17 for SQL Server};Server=legacy-server;Database=OldSystem;Trusted_Connection=yes;"
}
```

**When to use ODBC:**
- Legacy systems without native .NET drivers
- Proprietary databases (DB2, Informix, Teradata, etc.)
- Special driver requirements

**Note:** Native providers are recommended over ODBC for better performance and features.

### Provider Migration

If you're migrating from ODBC to native providers, update your configuration:

**Before (ODBC):**
```json
{
  "name": "MyDatabase",
  "connectionString": "Driver={ODBC Driver 17 for SQL Server};Server=localhost;Database=MyApp;Trusted_Connection=yes;"
}
```

**After (Native):**
```json
{
  "name": "MyDatabase",
  "provider": "sqlserver",
  "connectionString": "Server=localhost;Database=MyApp;Integrated Security=true;"
}
```

**Benefits of native providers:**
- Better performance (no ODBC translation layer)
- Native async/await support
- Provider-specific features and data types
- Better error messages and debugging
- No ODBC driver installation required

## Query Configuration

Queries define what SQL to execute, when to run it, and where to send results.

### Basic Query Example
```json
{
  "name": "Inventory Check",
  "cron": "0 */15 * * * ?",
  "database": "MySqlDb",
  "endpoint": "SimpleEndpoint",
  "enabled": true,
  "queryText": "SELECT * FROM Products WHERE StockLevel < ReorderLevel"
}
```

### Query Configuration Options

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `name` | string | Yes | Unique identifier for the query |
| `cron` | string | Yes | Cron expression for scheduling (Quartz format) |
| `database` | string | Yes | Name of database from databases section |
| `endpoint` | string | Yes | Name of endpoint from endpoints section |
| `enabled` | boolean | No | Enable/disable query (default: true) |
| `runOnStartup` | boolean | No | Run immediately on application start |
| `timeoutSeconds` | int | No | Query timeout in seconds (default: 30) |
| `maxRows` | int | No | Maximum rows to return (default: unlimited) |
| `payloadFormat` | string | No | "JsonArray" or "JsonLines" (default: JsonArray) |
| `queryText` | string | No* | Inline SQL query text |
| `queryFile` | string | No* | Path to SQL file |
| `onFailure` | string | No | "Ignore", "SlackAlert", or "EmailAlert" |

*Either `queryText` or `queryFile` must be specified

### Cron Expression Format

QueryPush uses Quartz.NET cron format (6 or 7 fields):

```
┌───────────── second (0 - 59)
│ ┌───────────── minute (0 - 59)
│ │ ┌───────────── hour (0 - 23)
│ │ │ ┌───────────── day of month (1 - 31)
│ │ │ │ ┌───────────── month (1 - 12)
│ │ │ │ │ ┌───────────── day of week (0 - 6) (Sunday=0)
│ │ │ │ │ │ ┌───────────── year (optional)
│ │ │ │ │ │ │
* * * * * * *
```

**Common Examples:**
- Every 15 minutes: `0 */15 * * * ?`
- Every hour at :00: `0 0 * * * ?`
- Daily at 6am: `0 0 6 * * ?`
- Every weekday at 9am: `0 0 9 ? * MON-FRI`
- First day of month at midnight: `0 0 0 1 * ?`

### Variable Substitution

QueryPush supports dynamic variables in queries using the `{VariableName}` syntax:

**Date/Time Variables:**
```sql
SELECT * FROM Orders
WHERE CreatedDate >= '{DateNow|-1:00:00|yyyy-MM-dd}'
```

Available formats:
- `{DateNow}` - Current date/time
- `{DateNow|yyyy-MM-dd}` - Formatted date
- `{DateNow|-1:00:00|yyyy-MM-dd}` - Date with offset (1 day ago)
- `{DateNow|+2:00:00|yyyy-MM-dd HH:mm:ss}` - 2 days in future

**State Variables:**
State variables persist across runs:
```sql
SELECT * FROM Events
WHERE EventId > {LastEventId}
```

Update state in your endpoint response or using state file.

## Endpoint Configuration

Endpoints define HTTP destinations for query results.

### Basic Endpoint
```json
{
  "name": "SimpleEndpoint",
  "method": "POST",
  "url": "https://webhook.site/your-unique-url"
}
```

### Advanced Endpoint
```json
{
  "name": "ProductionAPI",
  "method": "POST",
  "url": "https://api.example.com/data",
  "retryAttempts": 5,
  "retryStrategy": "ExponentialBackoff",
  "backOffSeconds": 10,
  "sendRequestIfNoResults": false,
  "payloadSize": 100,
  "requestDelay": 1000,
  "headers": [
    {
      "name": "Authorization",
      "value": "Bearer {Env:API_TOKEN}"
    },
    {
      "name": "Content-Type",
      "value": "application/json"
    }
  ]
}
```

### Endpoint Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `method` | string | POST | HTTP method |
| `url` | string | Required | Target URL |
| `retryAttempts` | int | 3 | Number of retry attempts |
| `retryStrategy` | string | Linear | "Linear" or "ExponentialBackoff" |
| `backOffSeconds` | int | 5 | Base retry delay in seconds |
| `sendRequestIfNoResults` | bool | true | Send request even with 0 rows |
| `payloadSize` | int | 1000 | Max rows per request |
| `requestDelay` | int | 0 | Delay between requests (ms) |
| `headers` | array | [] | Custom HTTP headers |

## Alert Configuration

Configure alerts for query failures:

### Slack Alerts
```json
{
  "slack": {
    "default": true,
    "webhookUrl": "https://hooks.slack.com/services/YOUR/SLACK/WEBHOOK",
    "channel": "#alerts",
    "username": "QueryPush",
    "alertCooldownMinutes": 60
  }
}
```

### Email Alerts
```json
{
  "email": {
    "smtpHost": "smtp.gmail.com",
    "smtpPort": 587,
    "useSsl": true,
    "from": "alerts@yourcompany.com",
    "to": "admin@yourcompany.com",
    "username": "alerts@yourcompany.com",
    "password": "your-app-password",
    "alertCooldownMinutes": 60
  }
}
```

## Logging Configuration

```json
{
  "logging": {
    "rotationStrategy": "Daily",
    "retentionDays": 30,
    "logDirectory": "logs"
  }
}
```

**Rotation Strategies:**
- `None` - Single log file
- `Daily` - Rotate daily
- `Weekly` - Rotate weekly
- `Monthly` - Rotate monthly

Logs are written to:
- Console (stdout)
- File: `{logDirectory}/querypush-{date}.log`
- Windows Event Log (Windows only)

## Deployment

### Windows Service Installation

1. Build self-contained executable:
   ```powershell
   dotnet publish -c Release -r win-x64 --self-contained
   ```

2. Install as service using `sc`:
   ```powershell
   sc create QueryPush binPath="C:\path\to\QueryPush.exe --service"
   sc start QueryPush
   ```

### Linux systemd Service

1. Build self-contained executable:
   ```bash
   dotnet publish -c Release -r linux-x64 --self-contained
   ```

2. Create systemd unit file `/etc/systemd/system/querypush.service`:
   ```ini
   [Unit]
   Description=QueryPush Database Query Scheduler
   After=network.target

   [Service]
   Type=notify
   ExecStart=/opt/querypush/QueryPush --service
   WorkingDirectory=/opt/querypush
   User=querypush
   Restart=always
   RestartSec=10

   [Install]
   WantedBy=multi-user.target
   ```

3. Enable and start:
   ```bash
   sudo systemctl daemon-reload
   sudo systemctl enable querypush
   sudo systemctl start querypush
   ```

## Build Scripts

### Build for All Platforms

You can create a build script to generate self-contained executables for multiple platforms:

**PowerShell (Windows):**
```powershell
# build-all.ps1
$platforms = @('win-x64', 'linux-x64', 'osx-x64', 'osx-arm64')

foreach ($platform in $platforms) {
    Write-Host "Building for $platform..."
    dotnet publish -c Release -r $platform --self-contained -o "publish/$platform"
}
```

**Bash (Linux/macOS):**
```bash
#!/bin/bash
# build-all.sh
platforms=("win-x64" "linux-x64" "osx-x64" "osx-arm64")

for platform in "${platforms[@]}"; do
    echo "Building for $platform..."
    dotnet publish -c Release -r $platform --self-contained -o "publish/$platform"
done
```

## Troubleshooting

### Database Connection Issues

1. **Provider not supported**: Verify the `provider` value is correct (case-insensitive)
2. **Driver missing**: For ODBC, ensure the ODBC driver is installed on the system
3. **Connection string**: Verify the connection string format for your provider
4. **Firewall**: Check that the database port is accessible
5. **Credentials**: Verify username/password or Windows Authentication settings

### Query Execution Issues

1. **Timeout errors**: Increase `timeoutSeconds` in query configuration
2. **Memory issues**: Use `maxRows` to limit result set size
3. **Variable substitution**: Check variable syntax and state file

### Endpoint Issues

1. **HTTP failures**: Check `retryAttempts` and `retryStrategy` settings
2. **Payload too large**: Reduce `payloadSize` to batch results
3. **Rate limiting**: Increase `requestDelay` between requests

## License

Copyright © 2025 Octane Software

## Support

For issues and questions, please open an issue on the GitHub repository.
