using SQLite;
using MyWorkSalary.Models;
using System.Globalization;
using MyWorkSalary.Services.Interfaces;

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

            // Skapa tabeller automatiskt
            _database.CreateTable<JobProfile>();
            _database.CreateTable<OBRate>();
            _database.CreateTable<WorkShift>();
            _database.CreateTable<AppSettings>();
            _database.CreateTable<FlexTimeBalance>();
        }

        #endregion

        #region FlexTimeBalance Methods

        /// <summary>
        /// Hämta flex-saldo för specifik månad
        /// </summary>
        public FlexTimeBalance GetFlexTimeBalance(int jobProfileId, int year, int month)
        {
            return _database.Table<FlexTimeBalance>()
                           .FirstOrDefault(x => x.JobProfileId == jobProfileId &&
                                               x.Year == year &&
                                               x.Month == month);
        }

        /// <summary>
        /// Hämta aktuellt totalt saldo (senaste månaden)
        /// </summary>
        public decimal GetCurrentFlexBalance(int jobProfileId)
        {
            var latest = _database.Table<FlexTimeBalance>()
                                 .Where(x => x.JobProfileId == jobProfileId)
                                 .OrderByDescending(x => x.Year)
                                 .ThenByDescending(x => x.Month)
                                 .FirstOrDefault();

            return latest?.RunningBalance ?? 0;
        }

        /// <summary>
        /// Hämta flex-historik för rapporter (nyaste först)
        /// </summary>
        public List<FlexTimeBalance> GetFlexTimeHistory(int jobProfileId)
        {
            return _database.Table<FlexTimeBalance>()
                           .Where(x => x.JobProfileId == jobProfileId)
                           .OrderByDescending(x => x.Year)
                           .ThenByDescending(x => x.Month)
                           .ToList();
        }

        /// <summary>
        /// Spara ny flex-balance
        /// </summary>
        public int SaveFlexTimeBalance(FlexTimeBalance flexBalance)
        {
            try
            {
                if (flexBalance.Id == 0)
                {
                    // Ny post - INSERT
                    return _database.Insert(flexBalance);
                }
                else
                {
                    // Befintlig post - UPDATE
                    flexBalance.ModifiedDate = DateTime.Now;
                    return _database.Update(flexBalance);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FEL i SaveFlexTimeBalance: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Uppdatera befintlig flex-balance (används när shifts ändras)
        /// </summary>
        public void UpdateFlexTimeBalance(FlexTimeBalance flexBalance)
        {
            try
            {
                flexBalance.ModifiedDate = DateTime.Now;
                _database.Update(flexBalance);

                // Räkna om alla efterföljande månader (kedjeeffekt)
                RecalculateRunningBalances(flexBalance.JobProfileId, flexBalance.Year, flexBalance.Month);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FEL i UpdateFlexTimeBalance: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Räkna om running balances från given månad och framåt
        /// (när en månad ändras påverkar det alla efterföljande månader)
        /// </summary>
        private void RecalculateRunningBalances(int jobProfileId, int fromYear, int fromMonth)
        {
            var allBalances = _database.Table<FlexTimeBalance>()
                                      .Where(x => x.JobProfileId == jobProfileId)
                                      .OrderBy(x => x.Year)
                                      .ThenBy(x => x.Month)
                                      .ToList();

            decimal runningTotal = 0;
            bool startRecalculating = false;

            foreach (var balance in allBalances)
            {
                if (balance.Year == fromYear && balance.Month == fromMonth)
                {
                    startRecalculating = true;
                }

                if (startRecalculating)
                {
                    if (balance.Year == fromYear && balance.Month == fromMonth)
                    {
                        // Första månaden - hämta föregående saldo
                        var previousBalance = GetPreviousFlexBalance(jobProfileId, fromYear, fromMonth);
                        runningTotal = previousBalance + balance.MonthlyDifference;
                    }
                    else
                    {
                        // Efterföljande månader
                        runningTotal += balance.MonthlyDifference;
                    }

                    balance.RunningBalance = runningTotal;
                    balance.ModifiedDate = DateTime.Now;
                    _database.Update(balance);
                }
                else
                {
                    runningTotal = balance.RunningBalance;
                }
            }
        }

        /// <summary>
        /// Hämta föregående månads saldo
        /// </summary>
        public decimal GetPreviousFlexBalance(int jobProfileId, int year, int month)
        {
            // Beräkna föregående månad
            var previousMonth = month - 1;
            var previousYear = year;

            if (previousMonth == 0)
            {
                previousMonth = 12;
                previousYear = year - 1;
            }

            var previous = _database.Table<FlexTimeBalance>()
                                   .Where(x => x.JobProfileId == jobProfileId &&
                                              x.Year < year ||
                                              (x.Year == year && x.Month < month))
                                   .OrderByDescending(x => x.Year)
                                   .ThenByDescending(x => x.Month)
                                   .FirstOrDefault();

            return previous?.RunningBalance ?? 0;
        }

        /// <summary>
        /// Ta bort flex-balance (och räkna om efterföljande)
        /// </summary>
        public int DeleteFlexTimeBalance(int id)
        {
            try
            {
                var balance = _database.Table<FlexTimeBalance>().FirstOrDefault(x => x.Id == id);
                if (balance != null)
                {
                    var result = _database.Delete<FlexTimeBalance>(id);

                    // Räkna om alla efterföljande månader
                    RecalculateRunningBalances(balance.JobProfileId, balance.Year, balance.Month);

                    return result;
                }
                return 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FEL i DeleteFlexTimeBalance: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region JobProfile Methods
        public List<JobProfile> GetJobProfiles()
        {
            return _database.Table<JobProfile>().ToList();
        }

        public JobProfile GetJobProfile(int id)
        {
            return _database.Table<JobProfile>().FirstOrDefault(x => x.Id == id);
        }

        public JobProfile GetActiveJob()
        {
            return _database.Table<JobProfile>().FirstOrDefault(x => x.IsActive);
        }

        public void SaveJobProfile(JobProfile jobProfile)
        {
            try
            {
                if (jobProfile.Id == 0)
                {
                    // Nytt jobb - INSERT
                    _database.Insert(jobProfile);
                }
                else
                {
                    // Befintligt jobb - UPDATE
                    var result = _database.Update(jobProfile);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FEL i SaveJobProfile: {ex.Message}");
                throw;
            }
        }

        public int DeleteJobProfile(int id)
        {
            try
            {
                // 1. Radera alla OB-regler för detta jobb FÖRST
                var obRates = _database.Table<OBRate>().Where(x => x.JobProfileId == id).ToList();
                foreach (var obRate in obRates)
                {
                    _database.Delete<OBRate>(obRate.Id);
                }

                // 2. Radera alla WorkShifts för detta jobb
                var workShifts = _database.Table<WorkShift>().Where(x => x.JobProfileId == id).ToList();
                foreach (var shift in workShifts)
                {
                    _database.Delete<WorkShift>(shift.Id);
                }

                // 3. Slutligen radera jobbet själv
                return _database.Delete<JobProfile>(id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FEL i DeleteJobProfile: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region OBRate Methods
        public List<OBRate> GetOBRates(int jobProfileId)
        {
            return _database.Table<OBRate>()
                   .Where(x => x.JobProfileId == jobProfileId)
                   .ToList();
        }

        public int SaveOBRate(OBRate obRate)
        {
            if (obRate.Id != 0)
            {
                return _database.Update(obRate);
            }
            else
            {
                return _database.Insert(obRate);
            }
        }

        public int DeleteOBRate(int id)
        {
            return _database.Delete<OBRate>(id);
        }

        #endregion

        #region Shift Methods
        public List<WorkShift> GetWorkShifts(int jobProfileId)
        {
            return _database.Table<WorkShift>()
                           .Where(x => x.JobProfileId == jobProfileId)
                           .ToList();
        }

        // Använd ShiftDate istället för StartTime för Semester/Sjuk
        public List<WorkShift> GetWorkShifts(int jobProfileId, DateTime fromDate, DateTime toDate)
        {
            return _database.Table<WorkShift>()
                           .Where(x => x.JobProfileId == jobProfileId &&
                                      x.ShiftDate >= fromDate &&
                                      x.ShiftDate <= toDate)
                           .ToList();
        }

        public int SaveWorkShift(WorkShift workShift)
        {
            if (workShift.Id != 0)
            {
                workShift.ModifiedDate = DateTime.Now;
                var result = _database.Update(workShift);
                return result;
            }
            else
            {
                var result = _database.Insert(workShift);
                return result;
            }
        }

        public int DeleteWorkShift(int id)
        {
            return _database.Delete<WorkShift>(id);
        }

        // Hämta pass för specifik månad (för rapporter)
        public List<WorkShift> GetWorkShiftsForMonth(int jobProfileId, int year, int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            return _database.Table<WorkShift>()
                           .Where(x => x.JobProfileId == jobProfileId &&
                                      x.ShiftDate >= startDate &&
                                      x.ShiftDate <= endDate)
                           .ToList();
        }

        // Hämta statistik för månad
        public (decimal TotalHours, decimal TotalPay, int TotalShifts) GetMonthlyStats(int jobProfileId, int year, int month)
        {
            var shifts = GetWorkShiftsForMonth(jobProfileId, year, month);

            var totalHours = shifts.Sum(x => x.TotalHours);
            var totalPay = shifts.Sum(x => x.TotalPay);
            var totalShifts = shifts.Count;

            return (totalHours, totalPay, totalShifts);
        }

        #endregion

        #region AppSettings Methods
        public AppSettings GetAppSettings()
        {
            var settings = _database.Table<AppSettings>().FirstOrDefault();

            if (settings == null)
            {
                settings = new AppSettings
                {
                    IsDarkTheme = false,
                    Language = "sv"
                };
                _database.Insert(settings);
            }
            return settings;
        }

        public void SaveAppSettings(AppSettings settings)
        {
            try
            {
                settings.LastModified = DateTime.Now;

                if (settings.Id == 0)
                {
                    _database.Insert(settings);
                }
                else
                {
                    _database.Update(settings);
                }

                _database.Execute("PRAGMA synchronous = FULL");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FEL i SaveAppSettings: {ex.Message}");
                throw;
            }
        }
        #endregion

        #region Database Management
        public void CloseConnection()
        {
            _database?.Close();
        }

        public void DeleteAllData()
        {
            _database.DeleteAll<WorkShift>();
            _database.DeleteAll<OBRate>();
            _database.DeleteAll<JobProfile>();
            _database.DeleteAll<AppSettings>();
        }
        #endregion

        #region Validering
        // Flytat till Services/Interfaces/IShiftValidationService.cs
        #endregion

        #region Konflikthantering - Detaljerad konfliktanalys
        // Flyttat till Services/Interfaces/IConflictResolutionService.cs
        #endregion

        #region Konfliktlösning - Automatiska åtgärder
        // Flyttat till Services/Interfaces/IConflictResolutionService.cs
        #endregion

        #region Hjälpfunktioner - Privata hjälpmetoder
        // Flyttat till Services/Interfaces/IConflictResolutionService.cs
        #endregion
    }
}
