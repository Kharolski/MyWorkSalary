using System.ComponentModel.DataAnnotations;

namespace MyWorkSalary.Models
{
    public class WorkShift
    {
        public int Id { get; set; }

        public int JobProfileId { get; set; }               // Kopplar till JobProfile

        [Required]
        public DateTime StartTime { get; set; }             // 2024-01-15 21:30

        [Required]
        public DateTime EndTime { get; set; }               // 2024-01-16 07:15

        public ShiftType ShiftType { get; set; }

        // Beräknade värden (sparas för snabbhet)
        public decimal TotalHours { get; set; }             // 9.75 timmar
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
