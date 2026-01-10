using SQLite;

namespace MyWorkSalary.Models.Specialized
{
    public class OBEvent
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        
        public int JobProfileId { get; set; }           // Länk till jobbet
        public int WorkShiftId { get; set; }           // Länk till passet 

        // När OB-timmarna arbetades
        public DateTime WorkDate { get; set; }  // T.ex. 2024-01-10
        public TimeSpan StartTime { get; set; } // T.ex. 18:00
        public TimeSpan EndTime { get; set; }   // T.ex. 22:00

        // OB-information
        public decimal Hours { get; set; }          // Antal OB-timmar (t.ex. 4.0)
        public string OBType { get; set; }          // "Kväll", "Natt", "Helg", osv.
        public decimal RatePerHour { get; set; }    // OB-tilläget per timme
        public decimal TotalAmount { get; set; }    // Beräknat totalt OB-tillägg (Hours * RatePerHour)

        // Metadata
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string Notes { get; set; }           // Eventuella anteckningar

        // Konstruktor för enkel skapelse
        public OBEvent() {   }

        // Hjälpmetod för att skapa en ny OBEvent
        public static OBEvent Create(int jobProfileId, int workShiftId,
                                   DateTime workDate, TimeSpan startTime, TimeSpan endTime,
                                   string obType, decimal ratePerHour, string notes = null)
        {
            var hours = (decimal)(endTime - startTime).TotalHours;

            return new OBEvent
            {
                JobProfileId = jobProfileId,
                WorkShiftId = workShiftId,
                WorkDate = workDate,
                StartTime = startTime,
                EndTime = endTime,
                Hours = hours,
                OBType = obType,
                RatePerHour = ratePerHour,
                TotalAmount = hours * ratePerHour,
                Notes = notes,
                CreatedAt = DateTime.Now
            };
        }
    }
}
