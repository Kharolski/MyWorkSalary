using SQLite;
using System.ComponentModel.DataAnnotations;
using MyWorkSalary.Models.Enums;

namespace MyWorkSalary.Models.Core
{
    public class WorkShift
    {
        #region Primary Key & Foreign Keys

        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int JobProfileId { get; set; }               // Kopplar till JobProfile

        #endregion

        #region Basic Shift Information

        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        [Required]
        public DateTime ShiftDate { get; set; }             // Datum för passet/perioden

        public ShiftType ShiftType { get; set; }            // Regular/SickLeave/VAB/Vacation/OnCall

        #endregion

        #region Regular Shift Calculations (sparas för snabbhet)

        public decimal TotalHours { get; set; }
        public decimal RegularHours { get; set; }           // Vanliga arbetstimmar
        public int BreakMinutes { get; set; } = 0;          // Rast i minuter (Regular shifts)
        public decimal OBHours { get; set; }                // OB-timmar totalt
        public decimal EveningHours { get; set; }
        public decimal NightHours { get; set; }
        public bool IsExtraShift { get; set; } = false;     // Markerar inbeordrat pass
        public bool IsHoliday { get; set; } = false;        // Markerar om passet är på röd dag / helgdag.
        public bool IsBigHoliday { get; set; } = false;

        // Debug / refactor-hjälp
        public decimal CalculatedOBHours => EveningHours + NightHours;
        #endregion

        #region Financial Calculations
        public decimal EveningOBRate { get; set; }
        public decimal NightOBRate { get; set; }
        public decimal EveningOBPay { get; set; }
        public decimal NightOBPay { get; set; }

        public decimal RegularPay { get; set; }             // Grundlön för timmarna
        public decimal OBPay { get; set; }                  // Totalt OB-tillägg
        public decimal TotalPay { get; set; }               // Summa

        // Debug / refactor-hjälp
        public decimal CalculatedOBPay => EveningOBPay + NightOBPay;
        #endregion

        #region Metadata

        public string? Notes { get; set; }                  // Anteckningar från användaren
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }
        public bool IsConfirmed { get; set; } = false;     // Användaren har verifierat

        #endregion

        #region Shift Rule Snapshot (att ShiftConverters fungerar rätt med icons)

        public bool EveningActiveAtThatTime { get; set; } = true;
        public bool NightActiveAtThatTime { get; set; } = true;

        #endregion

        #region TEMPORÄRA FÄLT (kommer flyttas till specialiserade klasser)

        public int? NumberOfDays { get; set; }              // <-- SKA FLYTTAS TILL Vacation/VAB

        #endregion
    }
}
