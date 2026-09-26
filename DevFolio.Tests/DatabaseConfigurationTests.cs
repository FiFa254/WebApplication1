using DevFolio.Infrastructure;
using Npgsql;

namespace DevFolio.Tests;

public class DatabaseConfigurationTests
{
    [Fact]
    public void NeonUrl_IsParsed_WithTls()
    {
        var result = new NpgsqlConnectionStringBuilder(DatabaseConfiguration.ParseDatabaseUrl(
            "postgresql://neondb_owner:p%40ss;word@ep-test.ap-southeast-1.aws.neon.tech/neondb?sslmode=require&channel_binding=require"));

        Assert.Equal("ep-test.ap-southeast-1.aws.neon.tech", result.Host);
        Assert.Equal(5432, result.Port);
        Assert.Equal("neondb", result.Database);
        Assert.Equal("neondb_owner", result.Username);
        Assert.Equal("p@ss;word", result.Password);
        Assert.Equal(SslMode.Require, result.SslMode);
    }

    [Fact]
    public void Url_WithoutSslMode_DefaultsToRequire()
    {
        var result = new NpgsqlConnectionStringBuilder(DatabaseConfiguration.ParseDatabaseUrl("postgres://u:p@db:6543/app"));

        Assert.Equal(6543, result.Port);
        Assert.Equal(SslMode.Require, result.SslMode);
    }

    [Fact]
    public void Url_CanDisableTls()
    {
        var result = new NpgsqlConnectionStringBuilder(DatabaseConfiguration.ParseDatabaseUrl("postgres://u:p@localhost:5432/app?sslmode=disable"));

        Assert.Equal(SslMode.Disable, result.SslMode);
    }

    [Fact]
    public void NonUrl_IsReturnedUnchanged()
    {
        const string connectionString = "Host=db;Database=app;Username=u;Password=p";

        Assert.Equal(connectionString, DatabaseConfiguration.ParseDatabaseUrl(connectionString));
    }
}
