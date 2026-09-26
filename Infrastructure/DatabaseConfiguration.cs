namespace DevFolio.Infrastructure;

public static class DatabaseConfiguration
{
    public enum DatabaseProvider
    {
        SqlServer,
        PostgreSql
    }

    public static DatabaseProvider ResolveProvider(IConfiguration configuration)
    {
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (!string.IsNullOrWhiteSpace(databaseUrl))
        {
            return DatabaseProvider.PostgreSql;
        }

        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        if (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase)
            || connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)
            || connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
        {
            return DatabaseProvider.PostgreSql;
        }

        return DatabaseProvider.SqlServer;
    }

    public static string GetConnectionString(IConfiguration configuration)
    {
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (!string.IsNullOrWhiteSpace(databaseUrl))
        {
            return ParseDatabaseUrl(databaseUrl);
        }

        return configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");
    }

    /// <summary>
    /// Converts postgres:// or postgresql:// URLs (e.g. from Neon, Render) to Npgsql format.
    /// </summary>
    public static string ParseDatabaseUrl(string databaseUrl)
    {
        if (!databaseUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            && !databaseUrl.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return databaseUrl;
        }

        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':', 2);

        var builder = new Npgsql.NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
            // Hosted databases (Neon, Render) need TLS; a URL can opt out with ?sslmode=disable (local / CI).
            SslMode = ReadSslMode(uri.Query)
        };

        return builder.ConnectionString;
    }

    private static Npgsql.SslMode ReadSslMode(string query)
    {
        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2 && parts[0].Equals("sslmode", StringComparison.OrdinalIgnoreCase)
                && Enum.TryParse<Npgsql.SslMode>(Uri.UnescapeDataString(parts[1]).Replace("-", ""), ignoreCase: true, out var mode))
            {
                return mode;
            }
        }

        return Npgsql.SslMode.Require;
    }
}
