using Microsoft.Data.SqlClient;

namespace EasyCore.Quartz
{
    /// <summary>
    /// Extension methods for configuring Quartz to use MySQL as the job store.
    /// </summary>
    public static class QuarztSqlServerExtensions
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
        public static void EasyCoreQuarztSqlServer(this QuarztOptions options, Action<SqlServerOptions> action)
        {
            if (action is null) return;

            var sqlServerOptions = new SqlServerOptions();

            action(sqlServerOptions);

            options.SetSqlServer(sqlServerOptions.ConnectionString);

            var dbName = ConnectionStringHelper.GetDatabaseName(sqlServerOptions.ConnectionString);

            if (dbName is null) throw new ArgumentException("Unable to determine the database name from the connection string.");

            var sqlScript = @$"USE [{dbName}];
                                                                                      
                                           IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'QRTZ_CALENDARS')
                                           BEGIN
                                               CREATE TABLE [dbo].[QRTZ_CALENDARS] (
                                                 [SCHED_NAME] NVARCHAR(120) NOT NULL,
                                                 [CALENDAR_NAME] NVARCHAR(200) NOT NULL,
                                                 [CALENDAR] VARBINARY(MAX) NOT NULL,
                                                 CONSTRAINT [PK_QRTZ_CALENDARS] PRIMARY KEY CLUSTERED ([SCHED_NAME], [CALENDAR_NAME])
                                               );
                                           END
                                                                                      
                                           IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'QRTZ_CRON_TRIGGERS')
                                           BEGIN
                                               CREATE TABLE [dbo].[QRTZ_CRON_TRIGGERS] (
                                                 [SCHED_NAME] NVARCHAR(120) NOT NULL,
                                                 [TRIGGER_NAME] NVARCHAR(150) NOT NULL,
                                                 [TRIGGER_GROUP] NVARCHAR(150) NOT NULL,
                                                 [CRON_EXPRESSION] NVARCHAR(120) NOT NULL,
                                                 [TIME_ZONE_ID] NVARCHAR(80) NULL,
                                                 CONSTRAINT [PK_QRTZ_CRON_TRIGGERS] PRIMARY KEY CLUSTERED ([SCHED_NAME], [TRIGGER_NAME], [TRIGGER_GROUP])
                                               );
                                           END
                                                                                     
                                           IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'QRTZ_FIRED_TRIGGERS')
                                           BEGIN
                                               CREATE TABLE [dbo].[QRTZ_FIRED_TRIGGERS] (
                                                 [SCHED_NAME] NVARCHAR(120) NOT NULL,
                                                 [ENTRY_ID] NVARCHAR(140) NOT NULL,
                                                 [TRIGGER_NAME] NVARCHAR(150) NOT NULL,
                                                 [TRIGGER_GROUP] NVARCHAR(150) NOT NULL,
                                                 [INSTANCE_NAME] NVARCHAR(200) NOT NULL,
                                                 [FIRED_TIME] BIGINT NOT NULL,
                                                 [SCHED_TIME] BIGINT NOT NULL,
                                                 [PRIORITY] INT NOT NULL,
                                                 [STATE] NVARCHAR(16) NOT NULL,
                                                 [JOB_NAME] NVARCHAR(150) NULL,
                                                 [JOB_GROUP] NVARCHAR(150) NULL,
                                                 [IS_NONCONCURRENT] BIT NULL,
                                                 [REQUESTS_RECOVERY] BIT NULL,
                                                 CONSTRAINT [PK_QRTZ_FIRED_TRIGGERS] PRIMARY KEY CLUSTERED ([SCHED_NAME], [ENTRY_ID])
                                               );
                                           END
                                                                                     
                                           IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'QRTZ_PAUSED_TRIGGER_GRPS')
                                           BEGIN
                                               CREATE TABLE [dbo].[QRTZ_PAUSED_TRIGGER_GRPS] (
                                                 [SCHED_NAME] NVARCHAR(120) NOT NULL,
                                                 [TRIGGER_GROUP] NVARCHAR(150) NOT NULL,
                                                 CONSTRAINT [PK_QRTZ_PAUSED_TRIGGER_GRPS] PRIMARY KEY CLUSTERED ([SCHED_NAME], [TRIGGER_GROUP])
                                               );
                                           END
                                                                                      
                                           IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'QRTZ_SCHEDULER_STATE')
                                           BEGIN
                                               CREATE TABLE [dbo].[QRTZ_SCHEDULER_STATE] (
                                                 [SCHED_NAME] NVARCHAR(120) NOT NULL,
                                                 [INSTANCE_NAME] NVARCHAR(200) NOT NULL,
                                                 [LAST_CHECKIN_TIME] BIGINT NOT NULL,
                                                 [CHECKIN_INTERVAL] BIGINT NOT NULL,
                                                 CONSTRAINT [PK_QRTZ_SCHEDULER_STATE] PRIMARY KEY CLUSTERED ([SCHED_NAME], [INSTANCE_NAME])
                                               );
                                           END
                                           
                                           IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'QRTZ_LOCKS')
                                           BEGIN
                                               CREATE TABLE [dbo].[QRTZ_LOCKS] (
                                                 [SCHED_NAME] NVARCHAR(120) NOT NULL,
                                                 [LOCK_NAME] NVARCHAR(40) NOT NULL,
                                                 CONSTRAINT [PK_QRTZ_LOCKS] PRIMARY KEY CLUSTERED ([SCHED_NAME], [LOCK_NAME])
                                               );
                                           END
                                                                                      
                                           IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'QRTZ_JOB_DETAILS')
                                           BEGIN
                                               CREATE TABLE [dbo].[QRTZ_JOB_DETAILS] (
                                                 [SCHED_NAME] NVARCHAR(120) NOT NULL,
                                                 [JOB_NAME] NVARCHAR(150) NOT NULL,
                                                 [JOB_GROUP] NVARCHAR(150) NOT NULL,
                                                 [DESCRIPTION] NVARCHAR(250) NULL,
                                                 [JOB_CLASS_NAME] NVARCHAR(250) NOT NULL,
                                                 [IS_DURABLE] BIT NOT NULL,
                                                 [IS_NONCONCURRENT] BIT NOT NULL,
                                                 [IS_UPDATE_DATA] BIT NOT NULL,
                                                 [REQUESTS_RECOVERY] BIT NOT NULL,
                                                 [JOB_DATA] VARBINARY(MAX) NULL,
                                                 CONSTRAINT [PK_QRTZ_JOB_DETAILS] PRIMARY KEY CLUSTERED ([SCHED_NAME], [JOB_NAME], [JOB_GROUP])
                                               );
                                           END
                                                                                      
                                           IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'QRTZ_SIMPLE_TRIGGERS')
                                           BEGIN
                                               CREATE TABLE [dbo].[QRTZ_SIMPLE_TRIGGERS] (
                                                 [SCHED_NAME] NVARCHAR(120) NOT NULL,
                                                 [TRIGGER_NAME] NVARCHAR(150) NOT NULL,
                                                 [TRIGGER_GROUP] NVARCHAR(150) NOT NULL,
                                                 [REPEAT_COUNT] INT NOT NULL,
                                                 [REPEAT_INTERVAL] BIGINT NOT NULL,
                                                 [TIMES_TRIGGERED] INT NOT NULL,
                                                 CONSTRAINT [PK_QRTZ_SIMPLE_TRIGGERS] PRIMARY KEY CLUSTERED ([SCHED_NAME], [TRIGGER_NAME], [TRIGGER_GROUP])
                                               );
                                           END
                                           
                                           IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'QRTZ_SIMPROP_TRIGGERS')
                                           BEGIN
                                               CREATE TABLE [dbo].[QRTZ_SIMPROP_TRIGGERS] (
                                                 [SCHED_NAME] NVARCHAR(120) NOT NULL,
                                                 [TRIGGER_NAME] NVARCHAR(150) NOT NULL,
                                                 [TRIGGER_GROUP] NVARCHAR(150) NOT NULL,
                                                 [STR_PROP_1] NVARCHAR(512) NULL,
                                                 [STR_PROP_2] NVARCHAR(512) NULL,
                                                 [STR_PROP_3] NVARCHAR(512) NULL,
                                                 [INT_PROP_1] INT NULL,
                                                 [INT_PROP_2] INT NULL,
                                                 [LONG_PROP_1] BIGINT NULL,
                                                 [LONG_PROP_2] BIGINT NULL,
                                                 [DEC_PROP_1] NUMERIC(13,4) NULL,
                                                 [DEC_PROP_2] NUMERIC(13,4) NULL,
                                                 [BOOL_PROP_1] BIT NULL,
                                                 [BOOL_PROP_2] BIT NULL,
                                                 [TIME_ZONE_ID] NVARCHAR(80) NULL,
                                                 CONSTRAINT [PK_QRTZ_SIMPROP_TRIGGERS] PRIMARY KEY CLUSTERED ([SCHED_NAME], [TRIGGER_NAME], [TRIGGER_GROUP])
                                               );
                                           END                                           
                                           
                                           IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'QRTZ_BLOB_TRIGGERS')
                                           BEGIN
                                               CREATE TABLE [dbo].[QRTZ_BLOB_TRIGGERS] (
                                                 [SCHED_NAME] NVARCHAR(120) NOT NULL,
                                                 [TRIGGER_NAME] NVARCHAR(150) NOT NULL,
                                                 [TRIGGER_GROUP] NVARCHAR(150) NOT NULL,
                                                 [BLOB_DATA] VARBINARY(MAX) NULL,
                                                 CONSTRAINT [PK_QRTZ_BLOB_TRIGGERS] PRIMARY KEY CLUSTERED ([SCHED_NAME], [TRIGGER_NAME], [TRIGGER_GROUP])
                                               );
                                           END
                                           
                                           IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'QRTZ_TRIGGERS')
                                           BEGIN
                                               CREATE TABLE [dbo].[QRTZ_TRIGGERS] (
                                                 [SCHED_NAME] NVARCHAR(120) NOT NULL,
                                                 [TRIGGER_NAME] NVARCHAR(150) NOT NULL,
                                                 [TRIGGER_GROUP] NVARCHAR(150) NOT NULL,
                                                 [JOB_NAME] NVARCHAR(150) NOT NULL,
                                                 [JOB_GROUP] NVARCHAR(150) NOT NULL,
                                                 [DESCRIPTION] NVARCHAR(250) NULL,
                                                 [NEXT_FIRE_TIME] BIGINT NULL,
                                                 [PREV_FIRE_TIME] BIGINT NULL,
                                                 [PRIORITY] INT NULL,
                                                 [TRIGGER_STATE] NVARCHAR(16) NOT NULL,
                                                 [TRIGGER_TYPE] NVARCHAR(8) NOT NULL,
                                                 [START_TIME] BIGINT NOT NULL,
                                                 [END_TIME] BIGINT NULL,
                                                 [CALENDAR_NAME] NVARCHAR(200) NULL,
                                                 [MISFIRE_INSTR] INT NULL,
                                                 [JOB_DATA] VARBINARY(MAX) NULL,
                                                 CONSTRAINT [PK_QRTZ_TRIGGERS] PRIMARY KEY CLUSTERED ([SCHED_NAME], [TRIGGER_NAME], [TRIGGER_GROUP])
                                               );
                                           END                                      
                                           
                                           IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_QRTZ_CRON_TRIGGERS_QRTZ_TRIGGERS')
                                           BEGIN
                                               ALTER TABLE [dbo].[QRTZ_CRON_TRIGGERS]
                                               ADD CONSTRAINT [FK_QRTZ_CRON_TRIGGERS_QRTZ_TRIGGERS] FOREIGN KEY ([SCHED_NAME], [TRIGGER_NAME], [TRIGGER_GROUP])
                                               REFERENCES [dbo].[QRTZ_TRIGGERS] ([SCHED_NAME], [TRIGGER_NAME], [TRIGGER_GROUP]) ON DELETE CASCADE;
                                           END
                                                                                   
                                           IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_QRTZ_SIMPLE_TRIGGERS_QRTZ_TRIGGERS')
                                           BEGIN
                                               ALTER TABLE [dbo].[QRTZ_SIMPLE_TRIGGERS]
                                               ADD CONSTRAINT [FK_QRTZ_SIMPLE_TRIGGERS_QRTZ_TRIGGERS] FOREIGN KEY ([SCHED_NAME], [TRIGGER_NAME], [TRIGGER_GROUP])
                                               REFERENCES [dbo].[QRTZ_TRIGGERS] ([SCHED_NAME], [TRIGGER_NAME], [TRIGGER_GROUP]) ON DELETE CASCADE;
                                           END
                                                                                      
                                           IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_QRTZ_SIMPROP_TRIGGERS_QRTZ_TRIGGERS')
                                           BEGIN
                                               ALTER TABLE [dbo].[QRTZ_SIMPROP_TRIGGERS]
                                               ADD CONSTRAINT [FK_QRTZ_SIMPROP_TRIGGERS_QRTZ_TRIGGERS] FOREIGN KEY ([SCHED_NAME], [TRIGGER_NAME], [TRIGGER_GROUP])
                                               REFERENCES [dbo].[QRTZ_TRIGGERS] ([SCHED_NAME], [TRIGGER_NAME], [TRIGGER_GROUP]) ON DELETE CASCADE;
                                           END
                                                                                   
                                           IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_QRTZ_TRIGGERS_QRTZ_JOB_DETAILS')
                                           BEGIN
                                               ALTER TABLE [dbo].[QRTZ_TRIGGERS]
                                               ADD CONSTRAINT [FK_QRTZ_TRIGGERS_QRTZ_JOB_DETAILS] FOREIGN KEY ([SCHED_NAME], [JOB_NAME], [JOB_GROUP])
                                               REFERENCES [dbo].[QRTZ_JOB_DETAILS] ([SCHED_NAME], [JOB_NAME], [JOB_GROUP]);
                                           END
                                                                                      
                                           IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_QRTZ_T_J' AND object_id = OBJECT_ID('dbo.QRTZ_TRIGGERS'))
                                           BEGIN
                                               CREATE INDEX IDX_QRTZ_T_J ON dbo.QRTZ_TRIGGERS(SCHED_NAME, JOB_NAME, JOB_GROUP);
                                           END
                                           
                                           IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_QRTZ_T_JG' AND object_id = OBJECT_ID('dbo.QRTZ_TRIGGERS'))
                                           BEGIN
                                               CREATE INDEX IDX_QRTZ_T_JG ON dbo.QRTZ_TRIGGERS(SCHED_NAME, JOB_GROUP);
                                           END
                                                                                      
                                           IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_QRTZ_T_C' AND object_id = OBJECT_ID('dbo.QRTZ_TRIGGERS'))
                                           BEGIN
                                               CREATE INDEX IDX_QRTZ_T_C ON dbo.QRTZ_TRIGGERS(SCHED_NAME, CALENDAR_NAME);
                                           END
                                                                                      
                                           IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_QRTZ_T_G' AND object_id = OBJECT_ID('dbo.QRTZ_TRIGGERS'))
                                           BEGIN
                                               CREATE INDEX IDX_QRTZ_T_G ON dbo.QRTZ_TRIGGERS(SCHED_NAME, TRIGGER_GROUP);
                                           END
                                                                                    
                                           IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_QRTZ_T_STATE' AND object_id = OBJECT_ID('dbo.QRTZ_TRIGGERS'))
                                           BEGIN
                                               CREATE INDEX IDX_QRTZ_T_STATE ON dbo.QRTZ_TRIGGERS(SCHED_NAME, TRIGGER_STATE);
                                           END
                                                                                   
                                           IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_QRTZ_T_N_STATE' AND object_id = OBJECT_ID('dbo.QRTZ_TRIGGERS'))
                                           BEGIN
                                               CREATE INDEX IDX_QRTZ_T_N_STATE ON dbo.QRTZ_TRIGGERS(SCHED_NAME, TRIGGER_NAME, TRIGGER_GROUP, TRIGGER_STATE);
                                           END
                                           
                                           IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_QRTZ_T_N_G_STATE' AND object_id = OBJECT_ID('dbo.QRTZ_TRIGGERS'))
                                           BEGIN
                                               CREATE INDEX IDX_QRTZ_T_N_G_STATE ON dbo.QRTZ_TRIGGERS(SCHED_NAME, TRIGGER_GROUP, TRIGGER_STATE);
                                           END
                                                                                     
                                           IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_QRTZ_T_NEXT_FIRE_TIME' AND object_id = OBJECT_ID('dbo.QRTZ_TRIGGERS'))
                                           BEGIN
                                               CREATE INDEX IDX_QRTZ_T_NEXT_FIRE_TIME ON dbo.QRTZ_TRIGGERS(SCHED_NAME, NEXT_FIRE_TIME);
                                           END
                                                                                      
                                           IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_QRTZ_T_NFT_ST' AND object_id = OBJECT_ID('dbo.QRTZ_TRIGGERS'))
                                           BEGIN
                                               CREATE INDEX IDX_QRTZ_T_NFT_ST ON dbo.QRTZ_TRIGGERS(SCHED_NAME, TRIGGER_STATE, NEXT_FIRE_TIME);
                                           END
                                                                                   
                                           IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_QRTZ_T_NFT_MISFIRE' AND object_id = OBJECT_ID('dbo.QRTZ_TRIGGERS'))
                                           BEGIN
                                               CREATE INDEX IDX_QRTZ_T_NFT_MISFIRE ON dbo.QRTZ_TRIGGERS(SCHED_NAME, MISFIRE_INSTR, NEXT_FIRE_TIME);
                                           END
                                                                                    
                                           IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_QRTZ_T_NFT_ST_MISFIRE' AND object_id = OBJECT_ID('dbo.QRTZ_TRIGGERS'))
                                           BEGIN
                                               CREATE INDEX IDX_QRTZ_T_NFT_ST_MISFIRE ON dbo.QRTZ_TRIGGERS(SCHED_NAME, MISFIRE_INSTR, NEXT_FIRE_TIME, TRIGGER_STATE);
                                           END
                                                                                      
                                           IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_QRTZ_T_NFT_ST_MISFIRE_GRP' AND object_id = OBJECT_ID('dbo.QRTZ_TRIGGERS'))
                                           BEGIN
                                               CREATE INDEX IDX_QRTZ_T_NFT_ST_MISFIRE_GRP ON dbo.QRTZ_TRIGGERS(SCHED_NAME, MISFIRE_INSTR, NEXT_FIRE_TIME, TRIGGER_GROUP,             TRIGGER_STATE);
                                           END
                                           
                                           IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_QRTZ_FT_TRIG_INST_NAME' AND object_id = OBJECT_ID('dbo.QRTZ_FIRED_TRIGGERS'))
                                           BEGIN
                                               CREATE INDEX IDX_QRTZ_FT_TRIG_INST_NAME ON dbo.QRTZ_FIRED_TRIGGERS(SCHED_NAME, INSTANCE_NAME);
                                           END
                                                                                  
                                           IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_QRTZ_FT_INST_JOB_REQ_RCVRY' AND object_id = OBJECT_ID ('dbo.QRTZ_FIRED_TRIGGERS'))
                                           BEGIN
                                               CREATE INDEX IDX_QRTZ_FT_INST_JOB_REQ_RCVRY ON dbo.QRTZ_FIRED_TRIGGERS(SCHED_NAME, INSTANCE_NAME, REQUESTS_RECOVERY);
                                           END
                                                                                     
                                           IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_QRTZ_FT_J_G' AND object_id = OBJECT_ID('dbo.QRTZ_FIRED_TRIGGERS'))
                                           BEGIN
                                               CREATE INDEX IDX_QRTZ_FT_J_G ON dbo.QRTZ_FIRED_TRIGGERS(SCHED_NAME, JOB_NAME, JOB_GROUP);
                                           END
                                                                                 
                                           IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_QRTZ_FT_JG' AND object_id = OBJECT_ID('dbo.QRTZ_FIRED_TRIGGERS'))
                                           BEGIN
                                               CREATE INDEX IDX_QRTZ_FT_JG ON dbo.QRTZ_FIRED_TRIGGERS(SCHED_NAME, JOB_GROUP);
                                           END
                                                                                      
                                           IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_QRTZ_FT_T_G' AND object_id = OBJECT_ID('dbo.QRTZ_FIRED_TRIGGERS'))
                                           BEGIN
                                               CREATE INDEX IDX_QRTZ_FT_T_G ON dbo.QRTZ_FIRED_TRIGGERS(SCHED_NAME, TRIGGER_NAME, TRIGGER_GROUP);
                                           END
                                                                                      
                                           IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_QRTZ_FT_TG' AND object_id = OBJECT_ID('dbo.QRTZ_FIRED_TRIGGERS'))
                                           BEGIN
                                               CREATE INDEX IDX_QRTZ_FT_TG ON dbo.QRTZ_FIRED_TRIGGERS(SCHED_NAME, TRIGGER_GROUP);
                                           END ";

            using SqlConnection connection = new SqlConnection(sqlServerOptions.ConnectionString);

            connection.Open();

            using SqlCommand command = new SqlCommand(sqlScript, connection);

            command.ExecuteNonQuery();

            connection.Close();
        }
    }
}
