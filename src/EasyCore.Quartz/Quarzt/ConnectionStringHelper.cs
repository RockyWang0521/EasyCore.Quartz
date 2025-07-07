using System.Data.Common;

namespace EasyCore.Quartz
{
    /// <summary>
    /// Provides helper methods for working with database connection strings.
    /// </summary>
    public static class ConnectionStringHelper
    {
        /// <summary>
        /// Attempts to extract the database name from a given connection string.
        /// </summary>
        /// <param name="connectionString">The full database connection string.</param>
        /// <returns>
        /// The name of the database if found; otherwise, <c>null</c>.
        /// </returns>
        public static string? GetDatabaseName(string connectionString)
        {
            var builder = new DbConnectionStringBuilder
            {
                ConnectionString = connectionString
            };

            var keys = new[] { "Initial Catalog", "Database", "Data Source", "DataSource", "database" };

            foreach (var key in keys)
            {
                if (builder.TryGetValue(key, out var value))
                {
                    return value?.ToString();
                }
            }

            return null;
        }
    }
}
