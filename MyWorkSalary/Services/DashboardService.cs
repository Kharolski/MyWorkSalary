using MyWorkSalary.Models;
using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Services.Interfaces;
using System.Data;
using System.Globalization;

namespace MyWorkSalary.Services
{
    public class DashboardService : IDashboardService
    {
        #region Private Fields
        private readonly DatabaseService _databaseService;
        #endregion

        #region Constructor
        public DashboardService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }
        #endregion

        #region Monthly Stats
        /// <summary>
        /// Beräknar statistik för aktuell månad för en given anställningsprofil.
        /// 
        /// Flexlogik:
        /// - Sjukdagar (hel eller delvis) drar bort hela planerade timmar från `ExpectedHours`.
        ///   Detta gör att sjukfrånvaro inte skapar skuldtimmar i flex.
        /// - Faktisk arbetstid på delvis sjukdagar registreras enbart för rapporter/statistik,
        ///   men påverkar inte flexbalansen.
        /// </summary>
        public MonthlyStats GetMonthlyStats(int jobProfileId)
        {
            var currentMonth = DateTime.Now;
            var monthStart = new DateTime(currentMonth.Year, currentMonth.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var shifts = _databaseService.WorkShifts.GetWorkShifts(jobProfileId)
                .Where(s => s.ShiftDate >= monthStart && s.ShiftDate <= monthEnd)
                .ToList();

            var jobProfile = _databaseService.JobProfiles.GetJobProfile(jobProfileId);
            var stats = new MonthlyStats
            {
                MonthStart = monthStart,
                MonthEnd = monthEnd
            };

            decimal hoursToDeduct = 0; // Samma för sjuk, VAB osv.

            foreach (var shift in shifts)
            {
                switch (shift.ShiftType)
                {
                    case ShiftType.Regular:
                        stats.TotalHours += shift.TotalHours;
                        stats.WorkDays++;
                        stats.RegularHours += shift.RegularHours;
                        stats.TotalObHours += CalculateObHoursForShift(shift);
                        break;

                    case ShiftType.SickLeave:
                        stats.SickDays++;

                        var sickLeave = _databaseService.SickLeaves.GetSickLeaveByWorkShiftId(shift.Id);

                        if (sickLeave != null)
                        {
                            if (sickLeave.SickType == SickLeaveType.ShouldHaveWorked)
                            {
                                // Hel dag sjuk - dra bort hela planerade tiden
                                hoursToDeduct += sickLeave.ScheduledHours;
                                stats.SickLeaveHours += sickLeave.ScheduledHours;
                            }
                            else if (sickLeave.SickType == SickLeaveType.WorkedPartially)
                            {
                                // Delvis sjuk - dra bara bort den TID MAN INTE JOBBAT
                                decimal scheduled = sickLeave.ScheduledHours;
                                decimal worked = sickLeave.WorkedHours;
                                decimal notWorked = scheduled - worked;

                                hoursToDeduct += notWorked; // Bara de timmar man inte jobbade

                                stats.SickLeaveHours += notWorked;
                                stats.TotalHours += worked; // Räkna arbetade timmar
                                stats.WorkDays++; // Räkna som arbetsdag
                            }
                            // Sjuk på ledighet - inget att dra bort
                        }
                        break;

                    case ShiftType.Vacation:
                        stats.TotalHours += shift.TotalHours;
                        stats.VacationDays++;
                        stats.RegularHours += shift.RegularHours;
                        break;

                    case ShiftType.VAB: // Vård av barn
                        stats.VabDays++;

                        var vabLeave = _databaseService.VABLeaves.GetByWorkShiftId(shift.Id);
                        if (vabLeave != null)
                        {
                            // Alltid delvis VAB - räkna bara bort de timmar man inte jobbat
                            decimal scheduled = vabLeave.ScheduledHours;
                            decimal worked = vabLeave.WorkedHours;
                            decimal notWorked = scheduled - worked;

                            hoursToDeduct += notWorked;
                            stats.VabHours += notWorked;
                            stats.TotalHours += worked;

                            // Räkna som arbetsdag om man jobbat något
                            if (worked > 0)
                                stats.WorkDays++;
                        }
                        break;
                }
            }

            // FLEXLOGIK: Beräkna förväntade timmar efter sjukfrånvaro
            if (jobProfile?.ExpectedHoursPerMonth > 0)
            {
                stats.ExpectedHours = jobProfile.ExpectedHoursPerMonth - hoursToDeduct;
                stats.FlexDifference = stats.TotalHours - stats.ExpectedHours;
                stats.OvertimeHours = Math.Max(0, stats.FlexDifference);

                stats.CurrentFlexBalance = GetCurrentFlexBalance(jobProfileId);
            }
            else
            {
                stats.ExpectedHours = 0;
                stats.FlexDifference = 0;
                stats.OvertimeHours = 0;
                stats.CurrentFlexBalance = 0;
            }

            return stats;
        }

        private decimal CalculateObHoursForShift(WorkShift shift)
        {
            if (shift.ShiftType != ShiftType.Regular || !shift.StartTime.HasValue || !shift.EndTime.HasValue)
                return 0;

            var start = shift.StartTime.Value;
            var end = shift.EndTime.Value;

            // Hantera pass över midnatt
            if (end < start)
                end = end.AddDays(1);

            decimal obHours = 0;
            var current = start;

            while (current < end)
            {
                var next = current.AddMinutes(1);
                var hour = current.TimeOfDay;

                // OB-tid: mellan 18:00 och 06:00
                if (hour >= new TimeSpan(18, 0, 0) || hour < new TimeSpan(6, 0, 0))
                    obHours += 1.0m / 60.0m; // 1 minut = 1/60 timme

                current = next;
            }

            return Math.Round(obHours, 2);
        }
        #endregion

        #region Recent Activities
        public List<RecentActivityItem> GetRecentActivities(int jobProfileId, int count = 4)
        {
            var shifts = _databaseService.WorkShifts.GetWorkShifts(jobProfileId)
                .OrderByDescending(s => s.ShiftDate)
                .ThenByDescending(s => s.StartTime)
                .Take(count)
                .ToList();

            var activities = new List<RecentActivityItem>();

            foreach (var shift in shifts)
            {
                var activity = new RecentActivityItem
                {
                    ActivityDate = shift.ShiftDate,
                    ActivityType = shift.ShiftType
                };

                switch (shift.ShiftType)
                {
                    case ShiftType.Regular:
                        activity.Icon = "🕐";
                        activity.TimeText = GetTimeDisplayText(shift);
                        activity.Description = "Arbetspass";
                        activity.Duration = $"({shift.TotalHours:F1}t)";
                        break;
                    case ShiftType.SickLeave:
                        activity.Icon = "🏥";
                        activity.TimeText = GetDateDisplayText(shift.ShiftDate);

                        // Hämta sjuktyp från SickLeave-objektet
                        var sickLeave = _databaseService.SickLeaves.GetSickLeaveByWorkShiftId(shift.Id);

                        if (sickLeave != null)
                        {
                            switch (sickLeave.SickType)
                            {
                                case SickLeaveType.ShouldHaveWorked:
                                    activity.Description = "Sjukdag (1 dag)";
                                    activity.Duration = $"({sickLeave.ScheduledHours:F1}t)";
                                    break;
                                case SickLeaveType.WorkedPartially:
                                    activity.Description = "Delvis sjuk";
                                    activity.Duration = $"({sickLeave.WorkedHours:F1}t)";
                                    break;
                                case SickLeaveType.WouldBeFree:
                                    activity.Description = "Sjuk på ledighet";
                                    activity.Duration = "(Ingen förlust)";
                                    break;
                            }
                        }
                        else
                        {
                            activity.Description = "Sjukdag";
                            activity.Duration = "(1 dag)";
                        }
                        break;
                    case ShiftType.Vacation:
                        activity.Icon = "🏖️";
                        activity.TimeText = GetDateDisplayText(shift.ShiftDate);
                        activity.Description = "Semester";
                        activity.Duration = "(1 dag)";
                        break;
                    case ShiftType.OnCall: // Jour
                        activity.Icon = "📞";
                        activity.TimeText = GetTimeDisplayText(shift);
                        activity.Description = "Jourpass";
                        activity.Duration = "Jour";
                        break;
                    case ShiftType.VAB: // Vård av barn
                        activity.Icon = "👶";
                        activity.TimeText = GetDateDisplayText(shift.ShiftDate);
                        activity.Description = "VAB";
                        activity.Duration = "(1 dag)";
                        break;
                }

                activities.Add(activity);
            }

            return activities;
        }
        #endregion

        #region Job Profile
        public JobProfile GetActiveJob()
        {
            var jobs = _databaseService.JobProfiles.GetJobProfiles();
            return jobs.FirstOrDefault(j => j.IsActive);
        }
        #endregion

        #region Today's Work
        public bool HasWorkedToday(int jobProfileId)
        {
            var today = DateTime.Today;
            var shifts = _databaseService.WorkShifts.GetWorkShifts(jobProfileId);
            return shifts.Any(s => s.ShiftDate.Date == today && s.ShiftType == ShiftType.Regular);
        }

        public decimal GetTodaysHours(int jobProfileId)
        {
            var today = DateTime.Today;
            var shifts = _databaseService.WorkShifts.GetWorkShifts(jobProfileId);
            return shifts
                .Where(s => s.ShiftDate.Date == today && s.ShiftType == ShiftType.Regular)
                .Sum(s => s.TotalHours);
        }
        #endregion

        #region Dashboard Summary
        public DashboardSummary GetDashboardSummary(int jobProfileId)
        {
            var monthlyStats = GetMonthlyStats(jobProfileId);
            var weeklyHours = GetWeeklyHours(jobProfileId);
            var flexStatus = GetFlexStatus(jobProfileId);

            return new DashboardSummary
            {
                HasWorkedToday = HasWorkedToday(jobProfileId),
                TodaysHours = GetTodaysHours(jobProfileId),
                WeeklyHours = weeklyHours,
                MonthlyHours = monthlyStats.TotalHours,
                MonthlyObHours = monthlyStats.TotalObHours,
                NextScheduledShift = GetNextScheduledShift(jobProfileId),
                HasUpcomingVacation = HasUpcomingVacation(jobProfileId),
                IsCurrentlySick = IsCurrentlySick(jobProfileId),

                // Flex-information
                CurrentFlexBalance = flexStatus.CurrentBalance,
                FlexBalanceText = flexStatus.BalanceText,
                FlexStatusIcon = flexStatus.StatusIcon
            };
        }
        #endregion

        #region FlexTime Methods 
        public decimal GetCurrentFlexBalance(int jobProfileId)
        {
            return _databaseService.FlexTime.GetCurrentFlexBalance(jobProfileId);
        }

        public decimal GetTotalFlexBalanceExcludingCurrentMonth(int jobProfileId)
        {
            return _databaseService.FlexTime.GetTotalFlexBalanceExcludingCurrentMonth(jobProfileId);
        }

        public List<FlexTimeBalance> GetFlexTimeHistory(int jobProfileId, int monthsBack = 12)
        {
            return _databaseService.FlexTime.GetFlexTimeHistory(jobProfileId)
                .Take(monthsBack)
                .ToList();
        }

        public void UpdateCurrentMonthFlexBalance(int jobProfileId)
        {
            var currentMonth = DateTime.Now;
            var year = currentMonth.Year;
            var month = currentMonth.Month;

            var jobProfile = _databaseService.JobProfiles.GetJobProfile(jobProfileId);

            // Bara för månadslön
            if (jobProfile?.ExpectedHoursPerMonth <= 0)
                return;

            // Hämta faktiska timmar för månaden
            var monthlyStats = GetMonthlyStats(jobProfileId);

            var actualHours = monthlyStats.TotalHours;
            var expectedHours = monthlyStats.ExpectedHours;
            var difference = actualHours - expectedHours;

            //System.Diagnostics.Debug.WriteLine($"[FLEX] Uppdaterar flex: expected={expectedHours}, actual={actualHours}, diff={difference}");

            // Hämta eller skapa FlexTimeBalance
            var existingBalance = _databaseService.FlexTime.GetFlexTimeBalance(jobProfileId, year, month);

            if (existingBalance == null)
            {
                // Skapa ny
                var previousBalance = _databaseService.FlexTime.GetPreviousFlexBalance(jobProfileId, year, month);

                var newBalance = new FlexTimeBalance
                {
                    JobProfileId = jobProfileId,
                    Year = year,
                    Month = month,
                    ExpectedHours = expectedHours,
                    ActualHours = actualHours,
                    MonthlyDifference = difference,
                    RunningBalance = previousBalance + difference
                };

                _databaseService.FlexTime.SaveFlexTimeBalance(newBalance);
            }
            else
            {
                // Uppdatera befintlig
                existingBalance.ExpectedHours = expectedHours;
                existingBalance.ActualHours = actualHours;
                existingBalance.MonthlyDifference = difference;

                // Räkna om running balance
                var previousBalance = _databaseService.FlexTime.GetPreviousFlexBalance(jobProfileId, year, month);
                existingBalance.RunningBalance = previousBalance + difference;

                _databaseService.FlexTime.UpdateFlexTimeBalance(existingBalance);
            }
        }

        public FlexStatus GetFlexStatus(int jobProfileId)
        {
            var currentBalance = GetCurrentFlexBalance(jobProfileId);
            var currentMonth = DateTime.Now;

            // Hämta denna månads difference
            var thisMonthBalance = _databaseService.FlexTime.GetFlexTimeBalance(jobProfileId, currentMonth.Year, currentMonth.Month);
            var monthlyDifference = thisMonthBalance?.MonthlyDifference ?? 0;

            return new FlexStatus
            {
                CurrentBalance = currentBalance,
                MonthlyDifference = monthlyDifference,
                BalanceText = FormatFlexBalance(currentBalance),
                MonthlyText = FormatMonthlyDifference(monthlyDifference),
                StatusIcon = GetFlexStatusIcon(currentBalance)
            };
        }
        #endregion

        #region Private Helper Methods
        private string GetTimeDisplayText(WorkShift shift)
        {
            if (shift.ShiftDate.Date == DateTime.Today)
            {
                return shift.StartTime.HasValue && shift.EndTime.HasValue
                    ? $"Idag {shift.StartTime:HH:mm}-{shift.EndTime:HH:mm}"
                    : "Idag";
            }
            else if (shift.ShiftDate.Date == DateTime.Today.AddDays(-1))
            {
                return shift.StartTime.HasValue && shift.EndTime.HasValue
                    ? $"Igår {shift.StartTime:HH:mm}-{shift.EndTime:HH:mm}"
                    : "Igår";
            }
            else
            {
                var dateText = shift.ShiftDate.ToString("d MMM", new CultureInfo("sv-SE"));
                return shift.StartTime.HasValue && shift.EndTime.HasValue
                    ? $"{dateText} {shift.StartTime:HH:mm}-{shift.EndTime:HH:mm}"
                    : dateText;
            }
        }

        private string GetDateDisplayText(DateTime date)
        {
            if (date.Date == DateTime.Today)
                return "Idag";
            else if (date.Date == DateTime.Today.AddDays(-1))
                return "Igår";
            else
                return date.ToString("d MMM", new CultureInfo("sv-SE"));
        }

        private decimal GetWeeklyHours(int jobProfileId)
        {
            var startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1); // Måndag
            var endOfWeek = startOfWeek.AddDays(6); // Söndag

            var shifts = _databaseService.WorkShifts.GetWorkShifts(jobProfileId);
            return shifts
                .Where(s => s.ShiftDate >= startOfWeek && s.ShiftDate <= endOfWeek && s.ShiftType == ShiftType.Regular)
                .Sum(s => s.TotalHours);
        }

        private string GetNextScheduledShift(int jobProfileId)
        {
            // Implementera om du har schemalagda pass i framtiden
            return null;
        }

        private bool HasUpcomingVacation(int jobProfileId)
        {
            var shifts = _databaseService.WorkShifts.GetWorkShifts(jobProfileId);
            return shifts.Any(s => s.ShiftDate > DateTime.Today && s.ShiftType == ShiftType.Vacation);
        }

        private bool IsCurrentlySick(int jobProfileId)
        {
            var shifts = _databaseService.WorkShifts.GetWorkShifts(jobProfileId);
            return shifts.Any(s => s.ShiftDate.Date == DateTime.Today && s.ShiftType == ShiftType.SickLeave);
        }

        private string FormatFlexBalance(decimal balance)
        {
            return balance switch
            {
                > 0 => $"+{balance:F1} tim kompledighet",
                < 0 => $"{balance:F1} tim skuld",
                _ => "Balanserat"
            };
        }

        private string FormatMonthlyDifference(decimal difference)
        {
            return difference switch
            {
                > 0 => $"+{difference:F1} timmar denna månad",
                < 0 => $"{difference:F1} timmar denna månad",
                _ => "Balanserat denna månad"
            };
        }

        private string GetFlexStatusIcon(decimal balance)
        {
            return balance switch
            {
                > 0 => "📈", // Kompledighet
                < 0 => "📉", // Skuld
                _ => "⚖️"    // Balanserat
            };
        }
        #endregion
    }
}
