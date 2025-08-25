using System;
using System.Collections.Generic;
using System.Linq;
using MyWorkSalary.Models.Core; // där Holiday ligger
using SQLite;

namespace MyWorkSalary.Services.Repositories
{
    public class HolidayRepository
    {
        #region Private Fields
        private readonly SQLiteConnection _db;
        #endregion

        #region Constructor
        public HolidayRepository(DatabaseService databaseService)
        {
            _db = databaseService.GetConnection();
        }
        #endregion

        #region Get Methods
        public List<Holiday> GetAll(string countryCode)
        {
            return _db.Table<Holiday>()
                      .Where(h => h.CountryCode == countryCode)
                      .OrderBy(h => h.Date)
                      .ToList();
        }

        public Holiday GetByDate(DateTime date, string countryCode)
        {
            var key = $"{date:yyyyMMdd}_{countryCode}";
            return _db.Find<Holiday>(key);
        }

        public bool IsHoliday(DateTime date, string countryCode)
        {
            var key = $"{date:yyyyMMdd}_{countryCode}";
            return _db.Find<Holiday>(key) != null;
        }
        #endregion

        #region Insert / Update Methods
        public int Insert(Holiday holiday)
        {
            if (string.IsNullOrEmpty(holiday.UniqueKey))
                holiday.UniqueKey = $"{holiday.Date:yyyyMMdd}_{holiday.CountryCode}";
            return _db.Insert(holiday);
        }

        public int InsertOrReplace(Holiday holiday)
        {
            if (string.IsNullOrEmpty(holiday.UniqueKey))
                holiday.UniqueKey = $"{holiday.Date:yyyyMMdd}_{holiday.CountryCode}";
            return _db.InsertOrReplace(holiday);
        }

        public int InsertOrReplaceAll(IEnumerable<Holiday> holidays, string countryCode)
        {
            int count = 0;
            foreach (var holiday in holidays)
            {
                holiday.CountryCode = countryCode;
                holiday.UniqueKey = $"{holiday.Date:yyyyMMdd}_{countryCode}";
                count += _db.InsertOrReplace(holiday);
            }
            return count;
        }
        #endregion

        #region Delete Methods
        public int Delete(Holiday holiday)
        {
            return _db.Delete(holiday);
        }

        public int DeleteByDate(DateTime date, string countryCode)
        {
            var key = $"{date:yyyyMMdd}_{countryCode}";
            return _db.Delete<Holiday>(key);
        }

        public int DeleteAllForCountry(string countryCode)
        {
            var holidays = _db.Table<Holiday>().Where(h => h.CountryCode == countryCode).ToList();
            int count = 0;
            foreach (var holiday in holidays)
            {
                count += _db.Delete(holiday);
            }
            return count;
        }
        #endregion
    }
}
