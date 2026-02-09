using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Specialized;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Services.Interfaces;
using MyWorkSalary.ViewModels.ShiftTypes;

namespace MyWorkSalary.Services.Handlers
{
    public class OnCallHandler
    {
        #region Fields
        private readonly IOnCallRepository _onCallRepository;
        private readonly IWorkShiftRepository _workShiftRepository;
        private readonly IJobProfileRepository _jobProfileRepository;
        private readonly IOnCallCalloutRepository _onCallCalloutRepository;
        #endregion

        #region Constructor
        public OnCallHandler(
            IOnCallRepository onCallRepository,
            IOnCallCalloutRepository onCallCalloutRepository,
            IWorkShiftRepository workShiftRepository,
            IJobProfileRepository jobProfileRepository)
        {
            _onCallRepository = onCallRepository;
            _onCallCalloutRepository = onCallCalloutRepository;
            _workShiftRepository = workShiftRepository;
            _jobProfileRepository = jobProfileRepository;
        }
        #endregion

        #region Create OnCall Shift
        public WorkShift CreateOnCallShift(
                int jobProfileId,
                DateTime shiftDate,
                TimeSpan standbyStartTime,
                TimeSpan standbyEndTime,
                IEnumerable<CalloutRow> callouts,
                string? notes = null)
        {
            var jobProfile = _jobProfileRepository.GetJobProfile(jobProfileId);
            if (jobProfile == null)
                throw new ArgumentException("JobProfile not found");

            // 1) Standby DateTimes (hantera över midnatt)
            var standbyStartDT = shiftDate.Date.Add(standbyStartTime);
            var standbyEndDT = shiftDate.Date.Add(standbyEndTime);
            if (standbyEndTime <= standbyStartTime)
                standbyEndDT = standbyEndDT.AddDays(1);

            // 2) Standby timmar
            var standbyHours = (decimal)(standbyEndDT - standbyStartDT).TotalHours;

            // 3) Aktiva timmar från callouts (TimeSpan, också midnatt-stöd)
            var activeHours = 0m;
            foreach (var c in callouts ?? Enumerable.Empty<CalloutRow>())
            {
                if (c == null)
                    continue;

                var s = c.Start;
                var e = c.End;

                // tomma / 0-längd ignoreras (valfritt)
                if (e == s)
                    continue;

                if (e <= s)
                    e = e.Add(TimeSpan.FromDays(1));

                activeHours += (decimal)(e - s).TotalHours;
            }

            // 4) Skapa WorkShift (Salary räknar pay senare)
            var workShift = new WorkShift
            {
                JobProfileId = jobProfileId,
                ShiftDate = shiftDate.Date,
                ShiftType = ShiftType.OnCall,

                StartTime = standbyStartDT,
                EndTime = standbyEndDT,

                TotalHours = Math.Round(activeHours, 2),
                TotalPay = 0m,          // <— viktigt: Salary tar lön/OB
                Notes = notes,
                CreatedDate = DateTime.Now
            };

            var savedWorkShift = _workShiftRepository.SaveWorkShift(workShift);

            // 5) Snapshot: aktiv timlön (om DefaultHourly)
            var defaultActiveRate = ResolveDefaultActiveHourlyRate(jobProfile);

            // 6) Skapa OnCallShift med snapshots
            var onCallShift = new OnCallShift
            {
                WorkShiftId = savedWorkShift.Id,
                StandbyStartTime = standbyStartTime,
                StandbyEndTime = standbyEndTime,
                StandbyHours = Math.Round(standbyHours, 2),

                StandbyPayTypeSnapshot = jobProfile.OnCallStandbyPayType,
                StandbyPayAmountSnapshot = jobProfile.OnCallStandbyPayAmount,

                ActivePayModeSnapshot = jobProfile.OnCallActivePayMode,
                ActiveHourlyRateSnapshot = jobProfile.OnCallActivePayMode == OnCallActivePayMode.CustomHourly
                    ? jobProfile.OnCallActiveHourlyRate
                    : defaultActiveRate,

                Notes = notes
            };

            _onCallRepository.Insert(onCallShift);

            // IMPORTANT: vi behöver Id för onCallShift → hämta tillbaka den
            // enklast: GetByWorkShiftId (du har redan)
            var persisted = _onCallRepository.GetByWorkShiftId(savedWorkShift.Id);
            if (persisted == null)
                throw new InvalidOperationException("Failed to persist OnCallShift");

            // 7) Spara callouts
            foreach (var c in callouts ?? Enumerable.Empty<CalloutRow>())
            {
                if (c == null)
                    continue;
                if (c.End == c.Start)
                    continue;

                _onCallCalloutRepository.Insert(new OnCallCallout
                {
                    OnCallShiftId = persisted.Id,
                    StartTime = c.Start,
                    EndTime = c.End,
                    Notes = c.Notes
                });
            }

            return savedWorkShift;
        }

        private decimal ResolveDefaultActiveHourlyRate(JobProfile jobProfile)
        {
            // Timanställd: använd hourly rate direkt
            if (jobProfile.EmploymentType == EmploymentType.Temporary)
                return jobProfile.HourlyRate ?? 0m;

            // Månadslön: räkna timlön från expected hours
            if (jobProfile.MonthlySalary > 0)
            {
                var monthlyHours = jobProfile.ExpectedHoursPerMonth > 0 ? jobProfile.ExpectedHoursPerMonth : 173.33m;
                return jobProfile.MonthlySalary.Value / monthlyHours;
            }

            return jobProfile.HourlyRate ?? 0m;
        }
        #endregion

        #region Get OnCall Data

        public OnCallShift GetOnCallDetails(int workShiftId)
        {
            return _onCallRepository.GetByWorkShiftId(workShiftId);
        }

        public List<OnCallShift> GetOnCallShiftsForJobProfile(int jobProfileId)
        {
            return _onCallRepository.GetByJobProfileId(jobProfileId);
        }

        #endregion

        #region Delete
        public bool DeleteOnCallShift(int workShiftId)
        {
            try
            {
                // 1) hitta OnCallShift
                var onCall = _onCallRepository.GetByWorkShiftId(workShiftId);
                if (onCall != null)
                {
                    // 2) ta bort callouts först
                    _onCallCalloutRepository.DeleteByOnCallShiftId(onCall.Id);

                    // 3) ta bort OnCallShift
                    _onCallRepository.Delete(onCall.Id);
                }

                // 4) ta bort WorkShift
                _workShiftRepository.DeleteWorkShift(workShiftId);

                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion
    }
}
