namespace EasyCore.Quartz
{
    public class QuarztOptions
    {
#pragma warning disable CS8618

        /// <summary>
        /// The connection string to the database.
        /// </summary>
        private string _connectionString;

#pragma warning restore CS8618

        /// <summary>
        /// The type of the SQL database.
        /// </summary>
        private SqlType _sqlType = SqlType.None;

        /// <summary>
        /// Node heartbeat interval time in seconds.
        /// </summary>
        public int CheckinMisfireThreshold { get; set; } = 10;

        /// <summary>
        /// After the heartbeat period, other nodes are allowed to take over its tasks, in seconds.
        /// </summary>
        public int CheckinInterval { get; set; } = 5;

        /// <summary>
        /// The maximum number of thread pool tasks which can be executing in parallel.
        /// </summary>
        public int MaxConcurrency { get; set; } = 20;

        /// <summary>
        /// The time zone offset (in hours) for scheduling jobs, relative to UTC.
        /// </summary>
        public int TimeZoneOffsetHours { get; set; } = 0;

        /// <summary>
        /// Gets the connection string and the type of the SQL database.
        /// </summary>
        /// <returns></returns>
        internal (string ConnectionString, SqlType SqlType) GetSettings()
            => (_connectionString, _sqlType);

        /// <summary>
        /// Sets the connection string and the type of the SQL database to SQL Server.
        /// </summary>
        /// <param name="connectionString"></param>
        public void SetSqlServer(string connectionString)
        {
            _connectionString = connectionString;
            _sqlType = SqlType.SqlServer;
        }

        /// <summary>
        /// Sets the connection string and the type of the SQL database to MySQL.
        /// </summary>
        /// <param name="connectionString"></param>
        public void SetMySql(string connectionString)
        {
            _connectionString = connectionString;
            _sqlType = SqlType.MySql;
        }

        /// <summary>
        /// Get SqlConnectionString.
        /// </summary>
        /// <param name="connectionString"></param>
        public string GetSqlConnectionString()
        {
            return _connectionString;
        }
    }

}
