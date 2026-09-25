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
        var username = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        var database = uri.AbsolutePath.TrimStart('/');
        var port = uri.Port > 0 ? uri.Port : 5432;

        return $"Host={uri.Host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
    }
}
