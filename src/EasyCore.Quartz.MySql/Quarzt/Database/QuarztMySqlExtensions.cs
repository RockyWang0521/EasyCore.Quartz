using MySql.Data.MySqlClient;

namespace EasyCore.Quartz
{
    /// <summary>
    /// Extension methods for configuring Quartz to use MySQL as the job store.
    /// </summary>
    public static class QuarztMySqlExtensions
    {
        /// <summary>
        /// Configures the Quartz options to use MySQL as the persistent store.
        /// 
        /// This method sets the database type and connection string on the <see cref="QuarztOptions"/>.
        /// It should be used during the EasyCoreQuartz setup to enable MySQL support.
        /// </summary>
        /// <param name="options">The Quartz configuration options.</param>
        /// <param name="action">
        /// A delegate to configure the MySQL connection settings.
        /// If null, the method exits without applying any configuration.
        /// </param>
        public static void EasyCoreQuarztMySql(this QuarztOptions options, Action<MySqlOptions> action)
        {
            if (action is null) return;

            var mySqlOptions = new MySqlOptions();

            action(mySqlOptions);

            options.SetMySql(mySqlOptions.ConnectionString);

            var dbName = ConnectionStringHelper.GetDatabaseName(mySqlOptions.ConnectionString);

            if (dbName is null) throw new ArgumentException("Unable to determine the database name from the connection string.");

            using var connection = new MySqlConnection(mySqlOptions.ConnectionString);

            connection.Open();

            bool TableExists(string tableName)
            {
                using var cmd = connection.CreateCommand();

                cmd.CommandText = @"
                SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_SCHEMA = @dbName AND TABLE_NAME = @tableName";

                cmd.Parameters.AddWithValue("@dbName", dbName);

                cmd.Parameters.AddWithValue("@tableName", tableName);

                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }

            bool IndexExists(string tableName, string indexName)
            {
                using var cmd = connection.CreateCommand();

                cmd.CommandText = @"
                SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
                WHERE TABLE_SCHEMA = @dbName AND TABLE_NAME = @tableName AND INDEX_NAME = @indexName";

                cmd.Parameters.AddWithValue("@dbName", dbName);

                cmd.Parameters.AddWithValue("@tableName", tableName);

                cmd.Parameters.AddWithValue("@indexName", indexName);

                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }

            var tablesSql = new Dictionary<string, string>
            {
                ["QRTZ_JOB_DETAILS"] = @"
                CREATE TABLE QRTZ_JOB_DETAILS(
                    SCHED_NAME VARCHAR(120) NOT NULL,
                    JOB_NAME VARCHAR(200) NOT NULL,
                    JOB_GROUP VARCHAR(200) NOT NULL,
                    DESCRIPTION VARCHAR(250) NULL,
                    JOB_CLASS_NAME VARCHAR(250) NOT NULL,
                    IS_DURABLE BOOLEAN NOT NULL,
                    IS_NONCONCURRENT BOOLEAN NOT NULL,
                    IS_UPDATE_DATA BOOLEAN NOT NULL,
                    REQUESTS_RECOVERY BOOLEAN NOT NULL,
                    JOB_DATA BLOB NULL,
                    PRIMARY KEY (SCHED_NAME,JOB_NAME,JOB_GROUP))
                ENGINE=InnoDB;",
                
                ["QRTZ_TRIGGERS"] = @"
                CREATE TABLE QRTZ_TRIGGERS (
                    SCHED_NAME VARCHAR(120) NOT NULL,
                    TRIGGER_NAME VARCHAR(200) NOT NULL,
                    TRIGGER_GROUP VARCHAR(200) NOT NULL,
                    JOB_NAME VARCHAR(200) NOT NULL,
                    JOB_GROUP VARCHAR(200) NOT NULL,
                    DESCRIPTION VARCHAR(250) NULL,
                    NEXT_FIRE_TIME BIGINT(19) NULL,
                    PREV_FIRE_TIME BIGINT(19) NULL,
                    PRIORITY INTEGER NULL,
                    TRIGGER_STATE VARCHAR(16) NOT NULL,
                    TRIGGER_TYPE VARCHAR(8) NOT NULL,
                    START_TIME BIGINT(19) NOT NULL,
                    END_TIME BIGINT(19) NULL,
                    CALENDAR_NAME VARCHAR(200) NULL,
                    MISFIRE_INSTR SMALLINT(2) NULL,
                    JOB_DATA BLOB NULL,
                    PRIMARY KEY (SCHED_NAME,TRIGGER_NAME,TRIGGER_GROUP),
                    FOREIGN KEY (SCHED_NAME,JOB_NAME,JOB_GROUP)
                    REFERENCES QRTZ_JOB_DETAILS(SCHED_NAME,JOB_NAME,JOB_GROUP))
                ENGINE=InnoDB;",
                
                ["QRTZ_SIMPLE_TRIGGERS"] = @"
                CREATE TABLE QRTZ_SIMPLE_TRIGGERS (
                    SCHED_NAME VARCHAR(120) NOT NULL,
                    TRIGGER_NAME VARCHAR(200) NOT NULL,
                    TRIGGER_GROUP VARCHAR(200) NOT NULL,
                    REPEAT_COUNT BIGINT(7) NOT NULL,
                    REPEAT_INTERVAL BIGINT(12) NOT NULL,
                    TIMES_TRIGGERED BIGINT(10) NOT NULL,
                    PRIMARY KEY (SCHED_NAME,TRIGGER_NAME,TRIGGER_GROUP),
                    FOREIGN KEY (SCHED_NAME,TRIGGER_NAME,TRIGGER_GROUP)
                    REFERENCES QRTZ_TRIGGERS(SCHED_NAME,TRIGGER_NAME,TRIGGER_GROUP))
                ENGINE=InnoDB;",
                
                ["QRTZ_CRON_TRIGGERS"] = @"
                CREATE TABLE QRTZ_CRON_TRIGGERS (
                    SCHED_NAME VARCHAR(120) NOT NULL,
                    TRIGGER_NAME VARCHAR(200) NOT NULL,
                    TRIGGER_GROUP VARCHAR(200) NOT NULL,
                    CRON_EXPRESSION VARCHAR(120) NOT NULL,
                    TIME_ZONE_ID VARCHAR(80),
                    PRIMARY KEY (SCHED_NAME,TRIGGER_NAME,TRIGGER_GROUP),
                    FOREIGN KEY (SCHED_NAME,TRIGGER_NAME,TRIGGER_GROUP)
                    REFERENCES QRTZ_TRIGGERS(SCHED_NAME,TRIGGER_NAME,TRIGGER_GROUP))
                ENGINE=InnoDB;",
                
                ["QRTZ_SIMPROP_TRIGGERS"] = @"
                CREATE TABLE QRTZ_SIMPROP_TRIGGERS (
                    SCHED_NAME VARCHAR(120) NOT NULL,
                    TRIGGER_NAME VARCHAR(200) NOT NULL,
                    TRIGGER_GROUP VARCHAR(200) NOT NULL,
                    STR_PROP_1 VARCHAR(512) NULL,
                    STR_PROP_2 VARCHAR(512) NULL,
                    STR_PROP_3 VARCHAR(512) NULL,
                    INT_PROP_1 INT NULL,
                    INT_PROP_2 INT NULL,
                    LONG_PROP_1 BIGINT NULL,
                    LONG_PROP_2 BIGINT NULL,
                    DEC_PROP_1 NUMERIC(13,4) NULL,
                    DEC_PROP_2 NUMERIC(13,4) NULL,
                    BOOL_PROP_1 BOOLEAN NULL,
                    BOOL_PROP_2 BOOLEAN NULL,
                    TIME_ZONE_ID VARCHAR(80),
                    PRIMARY KEY (SCHED_NAME,TRIGGER_NAME,TRIGGER_GROUP),
                    FOREIGN KEY (SCHED_NAME,TRIGGER_NAME,TRIGGER_GROUP) 
                    REFERENCES QRTZ_TRIGGERS(SCHED_NAME,TRIGGER_NAME,TRIGGER_GROUP))
                ENGINE=InnoDB;",
                
                ["QRTZ_BLOB_TRIGGERS"] = @"
                CREATE TABLE QRTZ_BLOB_TRIGGERS (
                    SCHED_NAME VARCHAR(120) NOT NULL,
                    TRIGGER_NAME VARCHAR(200) NOT NULL,
                    TRIGGER_GROUP VARCHAR(200) NOT NULL,
                    BLOB_DATA BLOB NULL,
                    PRIMARY KEY (SCHED_NAME,TRIGGER_NAME,TRIGGER_GROUP),
                    INDEX (SCHED_NAME,TRIGGER_NAME, TRIGGER_GROUP),
                    FOREIGN KEY (SCHED_NAME,TRIGGER_NAME,TRIGGER_GROUP)
                    REFERENCES QRTZ_TRIGGERS(SCHED_NAME,TRIGGER_NAME,TRIGGER_GROUP))
                ENGINE=InnoDB;",
                
                ["QRTZ_CALENDARS"] = @"
                CREATE TABLE QRTZ_CALENDARS (
                    SCHED_NAME VARCHAR(120) NOT NULL,
                    CALENDAR_NAME VARCHAR(200) NOT NULL,
                    CALENDAR BLOB NOT NULL,
                    PRIMARY KEY (SCHED_NAME,CALENDAR_NAME))
                ENGINE=InnoDB;",
                
                ["QRTZ_PAUSED_TRIGGER_GRPS"] = @"
                CREATE TABLE QRTZ_PAUSED_TRIGGER_GRPS (
                    SCHED_NAME VARCHAR(120) NOT NULL,
                    TRIGGER_GROUP VARCHAR(200) NOT NULL,
                    PRIMARY KEY (SCHED_NAME,TRIGGER_GROUP))
                ENGINE=InnoDB;",
                
                ["QRTZ_FIRED_TRIGGERS"] = @"
                CREATE TABLE QRTZ_FIRED_TRIGGERS (
                    SCHED_NAME VARCHAR(120) NOT NULL,
                    ENTRY_ID VARCHAR(140) NOT NULL,
                    TRIGGER_NAME VARCHAR(200) NOT NULL,
                    TRIGGER_GROUP VARCHAR(200) NOT NULL,
                    INSTANCE_NAME VARCHAR(200) NOT NULL,
                    FIRED_TIME BIGINT(19) NOT NULL,
                    SCHED_TIME BIGINT(19) NOT NULL,
                    PRIORITY INTEGER NOT NULL,
                    STATE VARCHAR(16) NOT NULL,
                    JOB_NAME VARCHAR(200) NULL,
                    JOB_GROUP VARCHAR(200) NULL,
                    IS_NONCONCURRENT BOOLEAN NULL,
                    REQUESTS_RECOVERY BOOLEAN NULL,
                    PRIMARY KEY (SCHED_NAME,ENTRY_ID))
                ENGINE=InnoDB;",
                
                ["QRTZ_SCHEDULER_STATE"] = @"
                CREATE TABLE QRTZ_SCHEDULER_STATE (
                    SCHED_NAME VARCHAR(120) NOT NULL,
                    INSTANCE_NAME VARCHAR(200) NOT NULL,
                    LAST_CHECKIN_TIME BIGINT(19) NOT NULL,
                    CHECKIN_INTERVAL BIGINT(19) NOT NULL,
                    PRIMARY KEY (SCHED_NAME,INSTANCE_NAME))
                ENGINE=InnoDB;",
                
                ["QRTZ_LOCKS"] = @"
                CREATE TABLE QRTZ_LOCKS (
                    SCHED_NAME VARCHAR(120) NOT NULL,
                    LOCK_NAME VARCHAR(40) NOT NULL,
                    PRIMARY KEY (SCHED_NAME,LOCK_NAME))
                ENGINE=InnoDB;"
            };

            var indexesSql = new Dictionary<string, (string TableName, string Sql)>
            {
                ["IDX_QRTZ_J_REQ_RECOVERY"] = ("QRTZ_JOB_DETAILS", "CREATE INDEX IDX_QRTZ_J_REQ_RECOVERY ON QRTZ_JOB_DETAILS(SCHED_NAME,REQUESTS_RECOVERY);"),
                ["IDX_QRTZ_J_GRP"] = ("QRTZ_JOB_DETAILS", "CREATE INDEX IDX_QRTZ_J_GRP ON QRTZ_JOB_DETAILS(SCHED_NAME,JOB_GROUP);"),

                ["IDX_QRTZ_T_J"] = ("QRTZ_TRIGGERS", "CREATE INDEX IDX_QRTZ_T_J ON QRTZ_TRIGGERS(SCHED_NAME,JOB_NAME,JOB_GROUP);"),
                ["IDX_QRTZ_T_JG"] = ("QRTZ_TRIGGERS", "CREATE INDEX IDX_QRTZ_T_JG ON QRTZ_TRIGGERS(SCHED_NAME,JOB_GROUP);"),
                ["IDX_QRTZ_T_C"] = ("QRTZ_TRIGGERS", "CREATE INDEX IDX_QRTZ_T_C ON QRTZ_TRIGGERS(SCHED_NAME,CALENDAR_NAME);"),
                ["IDX_QRTZ_T_G"] = ("QRTZ_TRIGGERS", "CREATE INDEX IDX_QRTZ_T_G ON QRTZ_TRIGGERS(SCHED_NAME,TRIGGER_GROUP);"),
                ["IDX_QRTZ_T_STATE"] = ("QRTZ_TRIGGERS", "CREATE INDEX IDX_QRTZ_T_STATE ON QRTZ_TRIGGERS(SCHED_NAME,TRIGGER_STATE);"),
                ["IDX_QRTZ_T_N_STATE"] = ("QRTZ_TRIGGERS", "CREATE INDEX IDX_QRTZ_T_N_STATE ON QRTZ_TRIGGERS(SCHED_NAME,TRIGGER_NAME,TRIGGER_GROUP,TRIGGER_STATE);"),
                ["IDX_QRTZ_T_N_G_STATE"] = ("QRTZ_TRIGGERS", "CREATE INDEX IDX_QRTZ_T_N_G_STATE ON QRTZ_TRIGGERS(SCHED_NAME,TRIGGER_GROUP,TRIGGER_STATE);"),
                ["IDX_QRTZ_T_NEXT_FIRE_TIME"] = ("QRTZ_TRIGGERS", "CREATE INDEX IDX_QRTZ_T_NEXT_FIRE_TIME ON QRTZ_TRIGGERS(SCHED_NAME,NEXT_FIRE_TIME);"),
                ["IDX_QRTZ_T_NFT_ST"] = ("QRTZ_TRIGGERS", "CREATE INDEX IDX_QRTZ_T_NFT_ST ON QRTZ_TRIGGERS(SCHED_NAME,TRIGGER_STATE,NEXT_FIRE_TIME);"),
                ["IDX_QRTZ_T_NFT_MISFIRE"] = ("QRTZ_TRIGGERS", "CREATE INDEX IDX_QRTZ_T_NFT_MISFIRE ON QRTZ_TRIGGERS(SCHED_NAME,MISFIRE_INSTR,NEXT_FIRE_TIME);"),
                ["IDX_QRTZ_T_NFT_ST_MISFIRE"] = ("QRTZ_TRIGGERS", "CREATE INDEX IDX_QRTZ_T_NFT_ST_MISFIRE ON QRTZ_TRIGGERS(SCHED_NAME,MISFIRE_INSTR,NEXT_FIRE_TIME,TRIGGER_STATE);"),
                ["IDX_QRTZ_T_NFT_ST_MISFIRE_GRP"] = ("QRTZ_TRIGGERS", "CREATE INDEX IDX_QRTZ_T_NFT_ST_MISFIRE_GRP ON QRTZ_TRIGGERS(SCHED_NAME,MISFIRE_INSTR,NEXT_FIRE_TIME,TRIGGER_GROUP,TRIGGER_STATE);"),

                ["IDX_QRTZ_FT_TRIG_INST_NAME"] = ("QRTZ_FIRED_TRIGGERS", "CREATE INDEX IDX_QRTZ_FT_TRIG_INST_NAME ON QRTZ_FIRED_TRIGGERS(SCHED_NAME,INSTANCE_NAME);"),
                ["IDX_QRTZ_FT_INST_JOB_REQ_RCVRY"] = ("QRTZ_FIRED_TRIGGERS", "CREATE INDEX IDX_QRTZ_FT_INST_JOB_REQ_RCVRY ON QRTZ_FIRED_TRIGGERS(SCHED_NAME,INSTANCE_NAME,REQUESTS_RECOVERY);"),
                ["IDX_QRTZ_FT_J_G"] = ("QRTZ_FIRED_TRIGGERS", "CREATE INDEX IDX_QRTZ_FT_J_G ON QRTZ_FIRED_TRIGGERS(SCHED_NAME,JOB_NAME,JOB_GROUP);"),
                ["IDX_QRTZ_FT_JG"] = ("QRTZ_FIRED_TRIGGERS", "CREATE INDEX IDX_QRTZ_FT_JG ON QRTZ_FIRED_TRIGGERS(SCHED_NAME,JOB_GROUP);"),
                ["IDX_QRTZ_FT_T_G"] = ("QRTZ_FIRED_TRIGGERS", "CREATE INDEX IDX_QRTZ_FT_T_G ON QRTZ_FIRED_TRIGGERS(SCHED_NAME,TRIGGER_NAME,TRIGGER_GROUP);"),
                ["IDX_QRTZ_FT_TG"] = ("QRTZ_FIRED_TRIGGERS", "CREATE INDEX IDX_QRTZ_FT_TG ON QRTZ_FIRED_TRIGGERS(SCHED_NAME,TRIGGER_GROUP);"),
            };

            foreach (var kvp in tablesSql)
            {
                var tableName = kvp.Key;

                var createSql = kvp.Value;

                if (!TableExists(tableName))
                {
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH}] EasyCore.Quartz: Creating table {tableName}...");

                    using var cmd = new MySqlCommand(createSql, connection);

                    cmd.ExecuteNonQuery();
                }
                else
                {
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH}] EasyCore.Quartz: Table {tableName} already exists. Skipping creation.");
                }
            }

            foreach (var kvp in indexesSql)
            {
                var indexName = kvp.Key;

                var tableName = kvp.Value.TableName;

                var createIndexSql = kvp.Value.Sql;

                if (!IndexExists(tableName, indexName))
                {
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH}] EasyCore.Quartz: Creating index {indexName} on table {tableName}...");

                    using var cmd = new MySqlCommand(createIndexSql, connection);

                    cmd.ExecuteNonQuery();
                }
                else
                {
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH}] EasyCore.Quartz: Index {indexName} on table {tableName} already exists. Skipping creation.");
                }
            }

            connection.Close(); 
        }
    }
}
