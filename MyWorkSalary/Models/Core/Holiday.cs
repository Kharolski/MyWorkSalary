using SQLite;

namespace MyWorkSalary.Models.Core
{
    [Table("Holidays")]
    public class Holiday
    {
        [PrimaryKey]
        public string UniqueKey { get; set; }   // t.ex. "20250606_SE"

        [Indexed]
        public DateTime Date { get; set; }

        public string Name { get; set; }

        [Indexed]
        public string CountryCode { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public static Holiday Create(DateTime date, string name, string countryCode)
        {
            return new Holiday
            {
                UniqueKey = $"{date:yyyyMMdd}_{countryCode}",
                Date = date,
                Name = name,
                CountryCode = countryCode,
                CreatedDate = DateTime.Now
            };
        }
    }
}
