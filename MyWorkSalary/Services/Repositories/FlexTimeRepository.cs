using MyWorkSalary.Models.Core;
using MyWorkSalary.Services.Interfaces;
using SQLite;

namespace MyWorkSalary.Services.Repositories
{
    public class FlexTimeRepository : IFlexTimeRepository
    {
        private readonly SQLiteConnection _database;

        public FlexTimeRepository(DatabaseService databaseService)
        {
            _database = databaseService.GetConnection();
        }

        #region Basic CRUD

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
                    flexBalance.CreatedDate = DateTime.Now;
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

        #region Business Queries
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

        #endregion

        #region Complex Calculations

        /// <summary>
        /// Räkna om running balances från given månad och framåt
        /// (när en månad ändras påverkar det alla efterföljande månader)
        /// </summary>
        public void RecalculateRunningBalances(int jobProfileId, int fromYear, int fromMonth)
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

        #endregion

        #region Validation & Utilities

        public bool HasFlexDataForMonth(int jobProfileId, int year, int month)
        {
            return _database.Table<FlexTimeBalance>()
                           .Any(x => x.JobProfileId == jobProfileId &&
                                    x.Year == year &&
                                    x.Month == month);
        }

        public List<FlexTimeBalance> GetFlexBalancesFromDate(int jobProfileId, int year, int month)
        {
            return _database.Table<FlexTimeBalance>()
                           .Where(x => x.JobProfileId == jobProfileId &&
                                      (x.Year > year || (x.Year == year && x.Month >= month)))
                           .OrderBy(x => x.Year)
                           .ThenBy(x => x.Month)
                           .ToList();
        }

        #endregion
    }
}
