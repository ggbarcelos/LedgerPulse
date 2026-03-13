using System.Data;
using System.Net.Sockets;
using LedgerPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace LedgerPulse.Infrastructure.Extensions;

public static class HostExtensions
{
    private const long MigrationLockKey = 4301202602;

    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        const int maxAttempts = 5;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var scope = serviceProvider.CreateScope();
            var logger = scope.ServiceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("DatabaseInitialization");
            var dbContext = scope.ServiceProvider.GetRequiredService<LedgerPulseDbContext>();
            var databaseEndpoint = DescribeDatabaseEndpoint(dbContext.Database.GetConnectionString());

            try
            {
                await using var migrationLock = await AcquireMigrationLockAsync(dbContext, cancellationToken: default);
                await BaselineEnsureCreatedDatabaseIfNeededAsync(dbContext, cancellationToken: default);
                await dbContext.Database.MigrateAsync();
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts && IsTransientStartupFailure(ex))
            {
                var retryDelay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                var failureReason = ex.GetBaseException().Message;

                logger.LogWarning(
                    "Failed to connect to PostgreSQL at {DatabaseEndpoint} during startup (attempt {Attempt}/{MaxAttempts}). Retrying in {RetryDelaySeconds} second(s). Reason: {FailureReason}",
                    databaseEndpoint,
                    attempt,
                    maxAttempts,
                    retryDelay.TotalSeconds,
                    failureReason);

                await Task.Delay(retryDelay);
            }
            catch (Exception ex) when (IsTransientStartupFailure(ex))
            {
                throw new InvalidOperationException(
                    $"Could not connect to PostgreSQL at {databaseEndpoint} during startup after {maxAttempts} attempts. Ensure the database is running and reachable, or start the local stack with 'docker compose up --build'.",
                    ex);
            }
        }
    }

    private static async Task BaselineEnsureCreatedDatabaseIfNeededAsync(
        LedgerPulseDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsNpgsql())
        {
            return;
        }

        if (await TableExistsAsync(dbContext, "__EFMigrationsHistory", cancellationToken))
        {
            return;
        }

        var applicationTablesExist =
            await TableExistsAsync(dbContext, "ledger_entries", cancellationToken) ||
            await TableExistsAsync(dbContext, "daily_ledger_summaries", cancellationToken) ||
            await TableExistsAsync(dbContext, "outbox_messages", cancellationToken);

        if (!applicationTablesExist)
        {
            return;
        }

        var initialMigrationId = dbContext.Database.GetMigrations().FirstOrDefault();
        if (string.IsNullOrWhiteSpace(initialMigrationId))
        {
            return;
        }

        var productVersion = typeof(DbContext).Assembly.GetName().Version?.ToString(3) ?? "10.0.0";

        await dbContext.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                "MigrationId" character varying(150) NOT NULL,
                "ProductVersion" character varying(32) NOT NULL,
                CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
            );
            """);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            VALUES ({0}, {1})
            ON CONFLICT ("MigrationId") DO NOTHING;
            """,
            initialMigrationId,
            productVersion);
    }

    private static async Task<IAsyncDisposable> AcquireMigrationLockAsync(
        LedgerPulseDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsNpgsql())
        {
            return AsyncDisposableAction.NoOp;
        }

        var connection = dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;
        if (shouldCloseConnection)
        {
            await dbContext.Database.OpenConnectionAsync(cancellationToken);
        }

        await using var acquireCommand = connection.CreateCommand();
        acquireCommand.CommandText = "SELECT pg_advisory_lock(@lockKey)";

        var lockParameter = acquireCommand.CreateParameter();
        lockParameter.ParameterName = "@lockKey";
        lockParameter.Value = MigrationLockKey;
        acquireCommand.Parameters.Add(lockParameter);

        await acquireCommand.ExecuteScalarAsync(cancellationToken);

        return new AsyncDisposableAction(async () =>
        {
            await using var releaseCommand = connection.CreateCommand();
            releaseCommand.CommandText = "SELECT pg_advisory_unlock(@lockKey)";

            var releaseParameter = releaseCommand.CreateParameter();
            releaseParameter.ParameterName = "@lockKey";
            releaseParameter.Value = MigrationLockKey;
            releaseCommand.Parameters.Add(releaseParameter);

            await releaseCommand.ExecuteScalarAsync();

            if (shouldCloseConnection)
            {
                await dbContext.Database.CloseConnectionAsync();
            }
        });
    }

    private static async Task<bool> TableExistsAsync(
        LedgerPulseDbContext dbContext,
        string tableName,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;
        if (shouldCloseConnection)
        {
            await dbContext.Database.OpenConnectionAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT EXISTS (
                    SELECT 1
                    FROM information_schema.tables
                    WHERE table_schema = 'public' AND table_name = @tableName
                )
                """;

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@tableName";
            parameter.Value = tableName;
            command.Parameters.Add(parameter);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is true;
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await dbContext.Database.CloseConnectionAsync();
            }
        }
    }

    private static bool IsTransientStartupFailure(Exception exception)
    {
        return exception switch
        {
            NpgsqlException => true,
            SocketException => true,
            TimeoutException => true,
            InvalidOperationException { InnerException: { } innerException } =>
                IsTransientStartupFailure(innerException),
            _ when exception.InnerException is { } innerException => IsTransientStartupFailure(innerException),
            _ => false
        };
    }

    private static string DescribeDatabaseEndpoint(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return "the configured PostgreSQL endpoint";
        }

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var databaseName = string.IsNullOrWhiteSpace(builder.Database) ? "<default>" : builder.Database;
        return $"{builder.Host}:{builder.Port}/{databaseName}";
    }

    private sealed class AsyncDisposableAction(Func<Task> disposeAction) : IAsyncDisposable
    {
        public static AsyncDisposableAction NoOp { get; } = new(() => Task.CompletedTask);

        public async ValueTask DisposeAsync()
        {
            await disposeAction();
        }
    }
}
