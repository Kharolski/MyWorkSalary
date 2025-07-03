using SQLite;
using System.ComponentModel.DataAnnotations;

namespace MyWorkSalary.Models
{
    public class WorkShift
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int JobProfileId { get; set; }               // Kopplar till JobProfile

        public DateTime? StartTime { get; set; }             // NULL för semester/sjuk
        public DateTime? EndTime { get; set; }               // NULL semester/sjuk

        [Required]
        public DateTime ShiftDate { get; set; }             // Datum för passet/perioden
        public ShiftType ShiftType { get; set; }

        // Semester/sjuk
        public int? NumberOfDays { get; set; }          // Antal dagar för Semester/Sjuk
        public decimal? SickPayPercentage { get; set; } // 80% efter karensdag
        public bool IsKarensDay { get; set; }           // Första dagen = karens

        // Beräknade värden (sparas för snabbhet)
        public decimal TotalHours { get; set; }             
        public decimal RegularHours { get; set; }           // Vanliga arbetstimmar
        public decimal OBHours { get; set; }                // OB-timmar totalt

        // Ekonomi
        public decimal RegularPay { get; set; }             // Grundlön för timmarna
        public decimal OBPay { get; set; }                  // Totalt OB-tillägg
        public decimal TotalPay { get; set; }               // Summa

        // Detaljerad OB-uppdelning (JSON eller separata tabeller)
        public string OBBreakdown { get; set; } = string.Empty;  // JSON med detaljer

        // Metadata
        public string? Notes { get; set; }                  // Anteckningar från användaren
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }
        public bool IsConfirmed { get; set; } = false;     // Användaren har verifierat
    }

    public enum ShiftType
    {
        Regular,        // Vanligt arbetspass
        Overtime,       // Övertid
        OnCall,         // Jour
        SickLeave,      // Sjukskrivning
        Vacation,       // Semester/ledighet
        Training        // Utbildning
    }
}
