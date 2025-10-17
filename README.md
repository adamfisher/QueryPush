# QueryPush 🛢⚡🌐

Cross-platform database query scheduler that executes database queries on a schedule and sends the records one at a time or in batches to the specified HTTP endpoints with comprehensive retry logic, alerting, and state management.

## Features

- **Multi-Database Support**: Native providers (SQL Server, MySQL, PostgreSQL, Oracle, SQLite) or ODBC for other databases
- **Cron Scheduling**: Standard cron expressions with NCrontab
- **HTTP Integration**: Configurable endpoints with headers and multiple HTTP methods
- **Template Variables**: Dynamic query parameters with formatting and offsets
- **Retry Logic**: Exponential backoff and delay strategies
- **Alert System**: Slack and Email notifications with throttling
- **State Persistence**: Last run tracking and alert cooldown management
- **Cross-Platform**: Windows Service, Linux Systemd, or console mode
- **Configuration Validation**: Startup validation with detailed error messages
- **Hot Reload**: JSON configuration changes without restart

## Getting Started

### Prerequisites

QueryPush supports both **native database providers** and **ODBC** for database connectivity.

**Native Providers (Recommended)**
Native providers are built into QueryPush and require no additional driver installation:
- **SQL Server** (`sqlserver`)
- **MySQL** (`mysql`)
- **PostgreSQL** (`postgres` or `postgresql`)
- **Oracle** (`oracle`)
- **SQLite** (`sqlite`)

**ODBC Provider**
For databases without native support, use the ODBC provider. **ODBC drivers must be installed on each target system**:
- **SQL Server**: [Microsoft ODBC Driver 17/18 for SQL Server](https://learn.microsoft.com/en-us/sql/connect/odbc/download-odbc-driver-for-sql-server)
- **MySQL**: [MySQL ODBC 8.0 Driver](https://dev.mysql.com/downloads/connector/odbc/)
- **PostgreSQL**: [PostgreSQL ODBC Driver (psqlODBC)](https://odbc.postgresql.org/)
- **Oracle**: [Oracle Instant Client ODBC Driver](https://www.oracle.com/database/technologies/instant-client/downloads.html)
- **SQLite**: [SQLite ODBC Driver](http://www.ch-werner.de/sqliteodbc/) (third-party)
- **MariaDB**: [MariaDB ODBC Driver](https://mariadb.com/downloads/connectors/connectors-data-access/odbc-connector/)

**ODBC Installation notes:**
- Drivers are platform-specific (Windows/Linux/macOS)
- Must match your system architecture (x64/x86)
- Required on every machine where QueryPush runs
- Driver versions may affect connection string syntax

### Quick Start

1. Download your platform-specific binary version from the [releases](https://github.com/adamfisher/QueryPush/releases) tab
2. If using ODBC, install the required drivers for your databases
3. Configure `appsettings.json` with connection strings for databases, the queries to execute, and the endpoints to send the data to (examples below)
4. Run `QueryPush.exe` at the command line or deploy it as a background service to continuously run

## Cross-Platform Deployment

### Console Mode (All Platforms)
```bash
dotnet run
# or
./QueryPush
```

### Windows Service
```cmd
QueryPush.exe --service
# Install using provided script:
install-windows.bat
```

### Linux Systemd Service
```bash
./QueryPush --service
# Install using provided script:
./install-linux.sh
```

### macOS (Console Mode)
```bash
./QueryPush
```

## Configuration Reference

QueryPush uses `appsettings.json` for all configuration. Below is a comprehensive reference of all available options:

### Database Configuration
| Property | Type | Required | Default | Description |
|----------|------|----------|---------|-------------|
| `databases[].name` | string | ✓ | | Unique database identifier |
| `databases[].provider` | enum | | `odbc` | Database provider: `odbc`, `sqlserver`, `mysql`, `oracle`, `postgres`/`postgresql`, `sqlite` |
| `databases[].connectionString` | string | ✓ | | Provider-specific connection string |

### Endpoint Configuration
| Property | Type | Required | Default | Description |
|----------|------|----------|---------|-------------|
| `endpoints[].name` | string | ✓ | | Unique endpoint identifier |
| `endpoints[].method` | enum | | `POST` | HTTP method: `GET`, `POST`, `PUT`, `PATCH`, `DELETE`, `HEAD`, `OPTIONS` |
| `endpoints[].url` | string | ✓ | | Target HTTP endpoint URL |
| `endpoints[].headers[].name` | string | ✓ | | HTTP header name |
| `endpoints[].headers[].value` | string | ✓ | | HTTP header value |
| `endpoints[].retryAttempts` | integer | | `3` | Max retry attempts (0-10) |
| `endpoints[].retryStrategy` | enum | | `Delay` | Retry strategy: `Delay`, `ExponentialBackoff` |
| `endpoints[].backOffSeconds` | integer | | `15` | Base delay between retries (1-300) |
| `endpoints[].sendRequestIfNoResults` | boolean | | `false` | Send HTTP request even with no data |
| `endpoints[].payloadSize` | integer | | `int.MaxValue` | Records per HTTP request (1-∞) |
| `endpoints[].requestDelay` | integer | | `500` | Milliseconds between requests (0-10000) |

### Alert Configuration
| Property | Type | Required | Default | Description |
|----------|------|----------|---------|-------------|
| `alerts.slack.default` | boolean | | `false` | Use as default alert method |
| `alerts.slack.webhookUrl` | string | ✓ | | Slack webhook URL |
| `alerts.slack.channel` | string | | `#alerts` | Slack channel |
| `alerts.slack.username` | string | | `QueryPush` | Bot username |
| `alerts.slack.alertCooldownMinutes` | integer | | `60` | Minutes between alerts (1-1440) |
| `alerts.email.smtpHost` | string | ✓ | | SMTP server hostname |
| `alerts.email.smtpPort` | integer | | `587` | SMTP server port (1-65535) |
| `alerts.email.useSsl` | boolean | | `true` | Enable SSL/TLS |
| `alerts.email.from` | string | ✓ | | Sender email address |
| `alerts.email.to` | string | ✓ | | Recipient email address |
| `alerts.email.username` | string | | | SMTP authentication username |
| `alerts.email.password` | string | | | SMTP authentication password |
| `alerts.email.alertCooldownMinutes` | integer | | `60` | Minutes between alerts (1-1440) |

### Logging Configuration
| Property | Type | Required | Default | Description |
|----------|------|----------|---------|-------------|
| `logging.rotationStrategy` | enum | | `Daily` | Log rotation: `Daily`, `Weekly`, `Monthly`, `Never` |
| `logging.retentionDays` | integer | | `30` | Days to retain logs (1-365) |
| `logging.logDirectory` | string | | `logs` | Log file directory path |

### Query Configuration
| Property | Type | Required | Default | Description |
|----------|------|----------|---------|-------------|
| `queries[].name` | string | ✓ | | Unique query identifier |
| `queries[].cron` | string | ✓ | | Standard cron expression ([Quartz Cron Generator](https://www.freeformatter.com/cron-expression-generator-quartz.html)) |
| `queries[].database` | string | ✓ | | Reference to database name |
| `queries[].endpoint` | string | ✓ | | Reference to endpoint name |
| `queries[].enabled` | boolean | | `true` | Enable/disable query |
| `queries[].runOnStartup` | boolean | | `true` | Execute immediately on startup |
| `queries[].timeoutSeconds` | integer | | `30` | Query timeout (1-3600) |
| `queries[].maxRows` | integer | | `int.MaxValue` | Maximum rows to process (1-∞) |
| `queries[].payloadFormat` | enum | | `JsonArray` | Data format: `JsonArray`, `JsonLines` |
| `queries[].onFailure` | enum | | `LogAndContinue` | Failure action: `LogAndContinue`, `Halt`, `SlackAlert`, `EmailAlert` |
| `queries[].queryText` | string | ✓* | | Inline SQL query text |
| `queries[].queryFile` | string | ✓* | | Path to external query file |

*Either `queryText` or `queryFile` is required

## Template Variables

QueryPush supports dynamic variables in query text:

| Variable | Description | Example Output |
|----------|-------------|----------------|
| `{DateTimeNow}` | Current local datetime | `2024-01-15 14:30:25` |
| `{UtcNow}` | Current UTC datetime | `2024-01-15 19:30:25` |
| `{DateNow}` | Current date only | `2024-01-15` |
| `{LastRun}` | Last successful execution | `2024-01-15 14:29:25` |
| `{Guid}` | New GUID | `550e8400-e29b-41d4-a716-446655440000` |
| `{MachineName}` | Host machine name | `SERVER01` |
| `{Env:VARIABLE}` | Environment variable | `Production` |

### Advanced Formatting

Variables support offset and formatting:
- **Offset**: `{DateNow\|-1:00:00\|yyyy-MM-dd}` → Yesterday's date
- **Format Only**: `{DateTimeNow\|yyyy-MM-dd HH:mm:ss}` → Custom format
- **Offset + Format**: `{UtcNow\|+05:00:00\|yyyy-MM-dd}` → 5 hours ahead

## How Scheduling Works

1. **QuartzSchedulerService** initializes Quartz.NET scheduler at startup
2. Each enabled query gets a dedicated Quartz job with its cron expression
3. Jobs execute independently when their cron schedule triggers
4. **QueryJob** handles individual query execution with correlation tracking
5. Results are processed, chunked, and sent to configured HTTP endpoints
6. State tracking prevents duplicate executions and manages alert cooldowns
7. Configuration changes trigger automatic rescheduling of all jobs

## Safety Features

- **Concurrent Execution Protection**: Each query is protected by `[DisallowConcurrentExecution]` - if a query is still running when its next scheduled time arrives, the new execution is skipped
- **State Persistence**: Last run timestamps prevent duplicate executions across application restarts
- **Database Connection Validation**: The `DatabaseConnectionFactory` validates connections at startup with detailed error messages for unsupported providers or connection failures
- **Configuration Validation**: Comprehensive validation of all settings, references, and file paths before execution begins

## State Management

QueryPush maintains state in `QueryState.json`:
- **Last run timestamps** per query (prevents duplicate execution)
- **Alert timestamps** per query/type (implements cooldown throttling)
- **Storage location**: Stored in the same directory as the application executable

## Example Configuration (Minimal)

Here is an example of a configuration with a single database query that runs daily at 9:00 AM using native SQL Server provider:

```json
{
  "databases": [
    {
      "name": "MyDb",
      "provider": "sqlserver",
      "connectionString": "Server=localhost;Database=MyApp;Integrated Security=true;"
    }
  ],
  "endpoints": [
    {
      "name": "MyWebhook",
      "url": "https://webhook.site/your-unique-url"
    }
  ],
  "queries": [
    {
      "name": "Daily Report",
      "cron": "0 0 9 * * ?",
      "database": "MyDb",
      "endpoint": "MyWebhook",
      "queryText": "SELECT * FROM Users WHERE CreatedDate >= '{DateNow|-1:00:00|yyyy-MM-dd}'"
    }
  ]
}
```

## Example Configuration

```json
{
  "databases": [
    {
      "name": "MainDb",
      "provider": "sqlserver",
      "connectionString": "Server=localhost;Database=MyApp;Integrated Security=true;"
    },
    {
      "name": "AnalyticsDb",
      "provider": "postgres",
      "connectionString": "Host=analytics.example.com;Database=analytics;Username=readonly;Password=secret;"
    }
  ],
  "endpoints": [
    {
      "name": "SyncAPI",
      "method": "POST",
      "url": "https://api.example.com/sync",
      "retryAttempts": 3,
      "retryStrategy": "Delay",
      "backOffSeconds": 15,
      "sendRequestIfNoResults": false,
      "payloadSize": 100,
      "requestDelay": 500,
      "headers": [
        {
          "name": "Authorization",
          "value": "Bearer {Env:API_TOKEN}"
        }
      ]
    }
  ],
  "alerts": {
    "slack": {
      "default": true,
      "webhookUrl": "https://hooks.slack.com/services/...",
      "channel": "#alerts"
    }
  },
  "queries": [
    {
      "name": "Daily User Sync",
      "cron": "0 0 6 * * ?",
      "database": "MainDb",
      "endpoint": "SyncAPI",
      "onFailure": "SlackAlert",
      "queryText": "SELECT Id, Email, CreatedAt FROM Users WHERE CreatedAt >= '{DateNow|-1:00:00|yyyy-MM-dd}'"
    }
  ]
}
```

## Logging

- **Console**: All platforms
- **Windows Event Log**: When running as service (`Application` log, source `QueryPush`)
- **Linux Journal**: Automatic via systemd integration
- **Configuration validation**: Detailed startup error messages

## Build & Publish

```bash
# Development build
dotnet build

# Platform-specific releases
dotnet publish -r win-x64 --self-contained -c Release
dotnet publish -r linux-x64 --self-contained -c Release
dotnet publish -r osx-x64 --self-contained -c Release
```
