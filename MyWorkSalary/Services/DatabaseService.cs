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
        public VacationLeaveRepository VacationLeaves { get; private set; }
        public OnCallShiftRepository OnCallShifts { get; private set; }
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
            Holidays = new HolidayRepository(this);
        }
        #endregion

        // Denna metod används för att helt ta bort VAB-relaterade data och tabeller, i syfte att "ta bort VAB-funktionen" från appen.
        private void RemoveVabCompletely()
        {
            try
            {
                // 1) Rensa ev. VABLeave data (om tabellen finns)
                _database.Execute("DELETE FROM VABLeave");

                // 2) Ta bort WorkShift-rader som är VAB.
                // OBS: VAB låg sist i enumen och var därför normalt int=4.
                _database.Execute("DELETE FROM WorkShift WHERE ShiftType = ?", 4);

                // 3) Droppa VABLeave-tabellen helt
                _database.Execute("DROP TABLE IF EXISTS VABLeave");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ RemoveVabCompletely error: {ex.Message}");
                // medvetet: vi kastar inte här för att inte blocka app-start
            }
        }

    }
}
