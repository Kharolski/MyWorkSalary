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

        /// <summary>
        /// Kontrollerar om ett datum är en helgdag (röd dag) för ett jobb i specifikt land
        /// </summary>
        public bool IsRedDay(DateTime date, JobProfile job)
        {
            if (job == null)
                return false;

            var countryCode = job.Country.GetCode();
            return IsHoliday(date, countryCode);
        }

        /// <summary>
        /// Kontrollerar om ett datum är en storhelg för ett jobb i specifikt land
        /// </summary>
        public bool IsBigHoliday(DateTime date, JobProfile job)
        {
            if (job == null)
                return false;

            var countryCode = job.Country.GetCode();
            var holiday = _holidayRepo.GetHoliday(date, countryCode);

            if (holiday == null)
                return false;

            return IsBigHolidayByName(holiday.Name, countryCode);
        }

        /// <summary>
        /// Hämtar både IsRedDay och IsBigHoliday för ett datum
        /// </summary>
        public (bool IsRedDay, bool IsBigHoliday) GetHolidayStatus(DateTime date, JobProfile job)
        {
            if (job == null)
                return (false, false);

            var isRedDay = IsRedDay(date, job);
            var isBigHoliday = isRedDay ? IsBigHoliday(date, job) : false;

            return (isRedDay, isBigHoliday);
        }

        /// <summary>
        /// Bestämmer om en helgdag är en storhelg baserat på namn och land
        /// </summary>
        private bool IsBigHolidayByName(string holidayName, string countryCode)
        {
            if (string.IsNullOrEmpty(holidayName))
                return false;

            // Stora helgar per land
            return countryCode switch
            {
                "SE" => IsSwedishBigHoliday(holidayName),
                "NO" => IsNorwegianBigHoliday(holidayName),
                "DK" => IsDanishBigHoliday(holidayName),
                _ => false
            };
        }
        #endregion

        #region Helpers
        private bool IsSwedishBigHoliday(string name)
        {
            var bigHolidays = new[]
            {
                "Juldagen", "Första juldagen", "Andra juldagen",
                "Påskdagen", "Annandag påsk",
                "Midsommarafton", "Midsommardagen",
                "Julafton", "Nyårsafton"
            };

            return bigHolidays.Any(h => name.Contains(h, StringComparison.OrdinalIgnoreCase));
        }
        private bool IsNorwegianBigHoliday(string name)
        {
            var bigHolidays = new[]
            {
                "Juledag", "Første juledag", "Andre juledag",
                "Påskedag", "Andre påskedag",
                "Julaften", "Nyttårsaften"
            };

            return bigHolidays.Any(h => name.Contains(h, StringComparison.OrdinalIgnoreCase));
        }
        private bool IsDanishBigHoliday(string name)
        {
            var bigHolidays = new[]
            {
                "Juledag", "Første juledag", "Anden juledag",
                "Påskedag", "Anden påskedag",
                "Juleaften", "Nytårsaften"
            };

            return bigHolidays.Any(h => name.Contains(h, StringComparison.OrdinalIgnoreCase));
        }
        #endregion
    }
}
