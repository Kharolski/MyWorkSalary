using SQLite;
using System.ComponentModel.DataAnnotations;
using MyWorkSalary.Models.Enums;

namespace MyWorkSalary.Models.Core
{
    public class WorkShift
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int JobProfileId { get; set; }               // Kopplar till JobProfile

        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        [Required]
        public DateTime ShiftDate { get; set; }             // Datum för passet/perioden

        public ShiftType ShiftType { get; set; }            // Regular/SickLeave/VAB/Vacation/OnCall

        // Beräknade värden (sparas för snabbhet)
        public decimal TotalHours { get; set; }
        public decimal RegularHours { get; set; }           // Vanliga arbetstimmar

        public int BreakMinutes { get; set; } = 0;          // Rast i minuter (Regular shifts)
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

        // === TEMPORÄRA FÄLT (kommer flyttas till specialiserade klasser) ===
        public int? NumberOfDays { get; set; }              // <-- FLYTTAS TILL Vacation/VAB
        public decimal? SickPayPercentage { get; set; }     // <-- FLYTTAS TILL SickLeave
        public bool IsKarensDay { get; set; }               // <-- FLYTTAS TILL SickLeave
        public decimal? WeeklyHoursUsed { get; set; }       // <-- FLYTTAS TILL SickLeave
        public decimal? HourlyRateUsed { get; set; }        // <-- FLYTTAS TILL SickLeave
        public decimal? WeeklyEarningsUsed { get; set; }    // <-- FLYTTAS TILL SickLeave
        public decimal? KarensDeduction { get; set; }       // <-- FLYTTAS TILL SickLeave
        public decimal? DailySickEarnings { get; set; }     // <-- FLYTTAS TILL SickLeave
        public int? SickPeriodId { get; set; }              // <-- FLYTTAS TILL SickLeave
        public bool IsRecurrentSickPeriod { get; set; } = false; // <-- FLYTTAS TILL SickLeave
        public decimal SickHours { get; set; } = 0;         // <-- FLYTTAS TILL SickLeave
        public decimal SickPay { get; set; } = 0;           // <-- FLYTTAS TILL SickLeave
    }
}
