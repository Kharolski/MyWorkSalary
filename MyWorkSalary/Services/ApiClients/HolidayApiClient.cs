using MyWorkSalary.Models.Core;
using System.Net.Http.Json;

namespace MyWorkSalary.Services.ApiClients
{
    public class HolidayApiClient
    {
        #region Private Fields
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://date.nager.at/api/v3";
        #endregion

        #region Constructor
        public HolidayApiClient(HttpClient httpClient = null)
        {
            _httpClient = httpClient ?? new HttpClient();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Hämtar helgdagar för ett specifikt år och land (t.ex. "SE" för Sverige)
        /// </summary>
        public async Task<List<Holiday>> GetPublicHolidaysAsync(int year, string countryCode)
        {
            try
            {
                var url = $"{BaseUrl}/PublicHolidays/{year}/{countryCode}";
                System.Diagnostics.Debug.WriteLine($"Anropar: {url}");

                var apiHolidays = await _httpClient.GetFromJsonAsync<List<NagerHolidayDto>>(url)
                                  ?? new List<NagerHolidayDto>();

                var holidays = apiHolidays
                    .Select(apiHoliday => Holiday.Create(apiHoliday.Date, apiHoliday.LocalName, countryCode))
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"API returnerade {holidays.Count} dagar");
                return holidays;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Fel vid hämtning av helgdagar: {ex.Message}");
                return new List<Holiday>();
            }
        }
        #endregion

        #region DTOs
        private class NagerHolidayDto
        {
            public DateTime Date { get; set; }
            public string LocalName { get; set; }
        }
        #endregion
    }
}
