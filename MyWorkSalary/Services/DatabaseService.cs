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
            _database.DeleteAll<SickLeave>();
            
            
            
        }

        private void CreateTables()
        {
            _database.CreateTable<AppSettings>();
            _database.CreateTable<OBRate>();
            _database.CreateTable<JobProfile>();
            _database.CreateTable<FlexTimeBalance>();
            _database.CreateTable<WorkShift>();
            _database.CreateTable<SickLeave>();
        }

        private void InitializeRepositories()
        {
            AppSettings = new AppSettingsRepository(this);
            OBRates = new OBRateRepository(this);
            JobProfiles = new JobProfileRepository(this);
            FlexTime = new FlexTimeRepository(this);
            WorkShifts = new WorkShiftRepository(this);
            SickLeaves = new SickLeaveRepository(this);
            
        }
        #endregion

    }
}
