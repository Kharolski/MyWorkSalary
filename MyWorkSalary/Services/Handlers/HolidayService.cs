using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Services.ApiClients;
using MyWorkSalary.Services.Repositories;
using System;
using System.Collections.Generic;

namespace MyWorkSalary.Services.Handlers
{
    public class HolidayService
    {
        #region Private Fields
        private readonly HolidayRepository _holidayRepo;
        private readonly HolidayApiClient _holidayApiClient;
        #endregion

        #region Constructor
        public HolidayService(HolidayRepository holidayRepository, HolidayApiClient holidayApiClient)
        {
            _holidayRepo = holidayRepository;
            _holidayApiClient = holidayApiClient;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Kolla om ett datum är en helgdag på specifik land
        /// </summary>
        public bool IsHoliday(DateTime date, string countryCode)
        {
            return _holidayRepo.IsHoliday(date, countryCode);
        }

        /// <summary>
        /// Hämta alla helgdagar för ett land
        /// </summary>
        public List<Holiday> GetAllHolidays(string countryCode)
        {
            return _holidayRepo.GetAll(countryCode);
        }

        /// <summary>
        /// Lägg till eller uppdatera en helgdag i ett land
        /// </summary>
        public void AddOrUpdateHoliday(Holiday holiday, string countryCode)
        {
            if (holiday == null)
                return;
            holiday.CountryCode = countryCode;
            holiday.UniqueKey = $"{holiday.Date:yyyyMMdd}_{countryCode}";
            _holidayRepo.InsertOrReplace(holiday);
        }

        /// <summary>
        /// Lägg till eller uppdatera flera helgdagar i ett land
        /// </summary>
        public void AddOrUpdateHolidays(IEnumerable<Holiday> holidays, string countryCode)
        {
            _holidayRepo.InsertOrReplaceAll(holidays, countryCode);
        }

        /// <summary>
        /// Ta bort en helgdag i ett land
        /// </summary>
        public void RemoveHoliday(DateTime date, string countryCode)
        {
            _holidayRepo.DeleteByDate(date, countryCode);
        }

        /// <summary>
        /// Hämtar helgdagar från API för ett visst år och landkod och sparar lokalt i databas
        /// </summary>
        public async Task SyncFromApiAsync(JobProfile job, int year)
        {
            if (job == null)
                throw new ArgumentNullException(nameof(job));

            var countryCode = job.Country.GetCode();
            var apiHolidays = await _holidayApiClient.GetPublicHolidaysAsync(year, countryCode);

            // Spara alla datum
            AddOrUpdateHolidays(apiHolidays, countryCode);

            var saved = _holidayRepo.GetAll(countryCode);
        }
        #endregion
    }
}
