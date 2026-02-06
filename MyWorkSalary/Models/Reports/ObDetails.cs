using MyWorkSalary.Models.Enums;

namespace MyWorkSalary.Models.Reports
{
    public class ObDetails
    {
        public DateTime Date { get; set; }           // Vilket datum skiftet var
        public decimal Hours { get; set; }           // Antal OB-timmar
        public decimal RatePerHour { get; set; }     // OB-taxa
        public OBCategory Category { get; set; }     // OB-kategori
        public OBDayType DayType { get; set; }
        public string DisplayName { get; set; } = "";   // namn på OB dagtyp
        public decimal Pay { get; set; }             // OB-lön
        public string PayText { get; set; } = "";    // detaljrad belopp

    }
}
