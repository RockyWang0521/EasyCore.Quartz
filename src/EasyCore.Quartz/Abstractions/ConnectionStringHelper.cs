using System.Data.Common;

namespace EasyCore.Quartz;

/// <summary>
/// Helpers for parsing database connection strings.
/// </summary>
public static class ConnectionStringHelper
{
    /// <summary>
    /// Attempts to extract a database name from a connection string.
    /// </summary>
    public static string? GetDatabaseName(string connectionString)
    {
        var builder = new DbConnectionStringBuilder
        {
            ConnectionString = connectionString
        };

        var keys = new[] { "Initial Catalog", "Database", "database" };

        foreach (var key in keys)
        {
            if (builder.TryGetValue(key, out var value) && value is not null)
            {
                var name = value.ToString();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    return name;
                }
            }
        }

        return null;
    }
}
