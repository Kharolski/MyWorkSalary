using SQLite;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Specialized;
using MyWorkSalary.Services.Repositories;

namespace MyWorkSalary.Services
{
    public class DatabaseService
    {
        #region Private Fields
        private SQLiteConnection _database;
        #endregion

        #region Constructor
        public DatabaseService(string dbPath)
        {
            _database = new SQLiteConnection(dbPath);
            CreateTables();

            //RunMigrations();

            InitializeRepositories();
        }
        #endregion

        #region Properties
        // Repositories som properties
        public AppSettingsRepository AppSettings { get; private set; }
        public OBRateRepository OBRates { get; private set; }
        public JobProfileRepository JobProfiles { get; private set; }
        public FlexTimeRepository FlexTime { get; private set; }
        public WorkShiftRepository WorkShifts { get; private set; }
        public SickLeaveRepository SickLeaves { get; private set; }
        public VacationLeaveRepository VacationLeaves { get; private set; }
        public OnCallShiftRepository OnCallShifts { get; private set; }
        public OnCallCalloutRepository OnCallCallouts { get; private set; }
        public HolidayRepository Holidays { get; private set; }

        #endregion

        #region Database Connection 

        /// <summary>
        /// Hämta database connection för repositories
        /// </summary>
        public SQLiteConnection GetConnection() => _database;

        #endregion

        #region Database Management
        public void CloseConnection()
        {
            _database?.Close();
        }

        public void DeleteAllData()
        {
            _database.DeleteAll<AppSettings>();
            _database.DeleteAll<OBRate>();
            _database.DeleteAll<JobProfile>();
            _database.DeleteAll<FlexTimeBalance>();
            _database.DeleteAll<WorkShift>();
            _database.DeleteAll<OBEvent>();
            _database.DeleteAll<SickLeave>();
            _database.DeleteAll<VacationLeave>();
            _database.DeleteAll<OnCallShift>();
            _database.CreateTable<OnCallCallout>();
            _database.DeleteAll<Holiday>();

        }

        private void CreateTables()
        {
            _database.CreateTable<AppSettings>();
            _database.CreateTable<OBRate>();
            _database.CreateTable<JobProfile>();
            _database.CreateTable<FlexTimeBalance>();
            _database.CreateTable<WorkShift>();
            _database.CreateTable<OBEvent>();
            _database.CreateTable<SickLeave>();
            _database.CreateTable<VacationLeave>();
            _database.CreateTable<OnCallShift>();
            _database.CreateTable<OnCallCallout>();
            _database.CreateTable<Holiday>();
        }

        private void InitializeRepositories()
        {
            AppSettings = new AppSettingsRepository(this);
            OBRates = new OBRateRepository(this);
            JobProfiles = new JobProfileRepository(this);
            FlexTime = new FlexTimeRepository(this);
            WorkShifts = new WorkShiftRepository(this);
            SickLeaves = new SickLeaveRepository(this);
            VacationLeaves = new VacationLeaveRepository(this);
            OnCallShifts = new OnCallShiftRepository(this);
            OnCallCallouts = new OnCallCalloutRepository(this);
            Holidays = new HolidayRepository(this);
        }
        #endregion

        #region Megrations
        private void RunMigrations()
        {
            try
            {
                // JobProfile: OnCall/jour columns
                EnsureColumnExists("JobProfile", "OnCallStandbyRatePerHour", "DECIMAL", "0");
                EnsureColumnExists("JobProfile", "OnCallActiveRatePerHour", "DECIMAL", "0");
                EnsureColumnExists("JobProfile", "OnCallAllowancePerShift", "DECIMAL", "0");
                EnsureColumnExists("JobProfile", "OnCallRecalcMonths", "INTEGER", "2");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ RunMigrations error: {ex.Message}");
                // medvetet: krascha inte vid start
            }
        }

        private void EnsureColumnExists(string tableName, string columnName, string columnType, string defaultSqlLiteral)
        {
            // PRAGMA table_info(TableName) -> lista kolumner
            var columns = _database.Query<TableInfoRow>($"PRAGMA table_info({tableName});");
            var exists = columns.Any(c => string.Equals(c.name, columnName, StringComparison.OrdinalIgnoreCase));
            if (exists)
                return;

            // SQLite tillåter ADD COLUMN med DEFAULT
            _database.Execute($"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnType} DEFAULT {defaultSqlLiteral}");
        }

        // hjälpklass för PRAGMA-resultat
        private class TableInfoRow
        {
            public int cid { get; set; }
            public string name { get; set; }
            public string type { get; set; }
            public int notnull { get; set; }
            public string dflt_value { get; set; }
            public int pk { get; set; }
        }
        #endregion

    }
}
